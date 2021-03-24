using System;
using System.Data;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif
using System.Linq;
using System.Text;

using PqsqlWrapper = Pqsql.UnsafeNativeMethods.PqsqlWrapper;

namespace Pqsql
{
	public abstract class PqsqlCopyBase : IDisposable
	{
		// connection for COPY
		protected PqsqlConnection mConn;

		// number of columns in the destination table
		protected int mColumns;

		// column datatype information for type inference
		internal PqsqlColInfo[] mRowInfo;

		public int CopyTimeout { get; set; }

		public string ColumnList { get; set; }

		public string Table { get; set; }

		protected abstract string CopyStmtDirection { get; }

		internal abstract ExecStatusType QueryResultType  { get; }

		protected string QueryInternal { get; set; }

		protected bool SuppressSchemaQueryInternal { get; set; }

		protected PqsqlCopyBase(PqsqlConnection conn)
		{
			mConn = conn;

			mRowInfo = null;
			mColumns = 0;
		}

		~PqsqlCopyBase()
		{
			Dispose(false);
		}

		#region Dispose

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool mDisposed;

		private void Dispose(bool disposing)
		{
			if (mDisposed)
			{
				return;
			}

			if (disposing)
			{
				mConn = null; // do not close connection
			}

			// always release mColBuf and mExpBuf (must not throw exception)
			Close();

			mDisposed = true;
		}

		#endregion

		public virtual void Start()
		{
#if CODECONTRACTS
			Contract.Requires<InvalidOperationException>(
				!string.IsNullOrWhiteSpace(Table) || !string.IsNullOrWhiteSpace(QueryInternal),
				"Table and Query properties are null.");
#else
			if (string.IsNullOrWhiteSpace(Table) && string.IsNullOrWhiteSpace(QueryInternal))
			{
				throw new InvalidOperationException("Table and Query properties are null.");
			}
#endif

			// PQexec does not set field types in PQresult for COPY FROM statements,
			// just retrieve 0 rows for the field types of Table or Query

			// TODO use PqsqlCopyColumnsCollection for ColumnList
			StringBuilder sb = new StringBuilder();
			if (!SuppressSchemaQueryInternal)
			{
				if (QueryInternal == null)
				{
					sb.AppendFormat("SELECT {0} FROM {1} LIMIT 0;", (object) ColumnList ?? '*' , Table);
				}
				else
				{
					sb.AppendFormat("{0} LIMIT 0;", QueryInternal);
				}

				// fetch number of columns and store column information
				using (PqsqlCommand cmd = new PqsqlCommand(mConn))
				{
					cmd.CommandText = sb.ToString();
					cmd.CommandType = CommandType.Text;
					cmd.CommandTimeout = CopyTimeout;

					using (PqsqlDataReader r = cmd.ExecuteReader(CommandBehavior.Default))
					{
						// just pick current row information
						PqsqlColInfo[] src = r.RowInformation;

						if (src == null)
						{
							throw new PqsqlException("Cannot retrieve RowInformation for '" +
													  QueryInternal ?? Table + "'.");
						}

						mColumns = src.Length;
						mRowInfo = new PqsqlColInfo[mColumns];

						Array.Copy(src, mRowInfo, mColumns);
					}
				}

				sb.Clear();
			}

			// now build COPY FROM statement
			sb.Append("COPY ");

			if (QueryInternal == null)
			{
				sb.AppendFormat("{0} ", Table);

				// always create list of columns if not SuppressSchemaQuery
				if (string.IsNullOrEmpty(ColumnList))
				{
					if (!SuppressSchemaQueryInternal)
					{
						// just assume that we use standard table order
						sb.AppendFormat("({0})", string.Join(",", mRowInfo.Select(r => r.ColumnName)));
					}
				}
				else
				{
					// let user decide the column order
					sb.AppendFormat("({0})", ColumnList);
				}
			}
			else
			{
				sb.AppendFormat("({0})", QueryInternal);
			}

			sb.AppendFormat(" {0} BINARY", CopyStmtDirection);

			byte[] q = PqsqlUTF8Statement.CreateUTF8Statement(sb);

			IntPtr res;
			ExecStatusType s = mConn.Exec(q, out res);

			// result buffer should contain column information and PGconn should be in COPY_IN state
			if (res == IntPtr.Zero || s != QueryResultType)
			{
				mConn.Consume(res); // we might receive several results...
				throw new PqsqlException("Could not execute statement «" + sb + "»: " + mConn.GetErrorMessage());
			}

			// check first column format, current implementation will have all columns set to binary 
			if (PqsqlWrapper.PQfformat(res, 0) == 0)
			{
				mConn.Consume(res);
				throw new PqsqlException("PqsqlCopyFrom only supports BINARY format.");
			}

			var nFields = PqsqlWrapper.PQnfields(res);
			if (SuppressSchemaQueryInternal)
			{
				mColumns = nFields;
			}
			else
			{
				// sanity check
				if (mColumns != nFields)
				{
					mConn.Consume(res);
					throw new PqsqlException("Received wrong number of columns for " + sb);
				}
			}

			// done with result inspection
			PqsqlWrapper.PQclear(res);
		}

		public abstract void Close();

		protected string Error()
		{
			IntPtr res;
			string err = string.Empty;
			IntPtr conn = mConn.PGConnection;

			res = PqsqlWrapper.PQgetResult(conn);

			if (res != IntPtr.Zero)
			{
				ExecStatusType s = PqsqlWrapper.PQresultStatus(res);

				PqsqlWrapper.PQclear(res);

				if (s == ExecStatusType.PGRES_COPY_IN || s == ExecStatusType.PGRES_COPY_OUT)
				{
					if (s == ExecStatusType.PGRES_COPY_IN)
					{
						// still in COPY_IN mode? bail out!
						byte[] b = PqsqlUTF8Statement.CreateUTF8Statement("COPY cancelled by client");
						int end;

						unsafe
						{
							fixed (byte* bs = b)
							{
								end = PqsqlWrapper.PQputCopyEnd(conn, bs);
							}
						}

						if (end != 1)
						{
							err = err.Insert(0, "Cannot cancel COPY (" + s + "): ");

							goto bailout;
						}
					}

					res = PqsqlWrapper.PQgetResult(conn);

					if (res != IntPtr.Zero)
					{
						s = PqsqlWrapper.PQresultStatus(res);
						PqsqlWrapper.PQclear(res);
					}
				}

				if (s != ExecStatusType.PGRES_COMMAND_OK)
				{
					err = err.Insert(0, "COPY failed (" + s + "): ");

					goto bailout;
				}

				// consume all remaining results until we reach the NULL result
				while ((res = PqsqlWrapper.PQgetResult(conn)) != IntPtr.Zero)
				{
					// always free mResult
					PqsqlWrapper.PQclear(res);
				}

				return err;
			}

		bailout:
			err += mConn.GetErrorMessage();

			return err;
		}
	}
}
