using System;
using System.Data;
using System.Diagnostics.Contracts;
using System.Text;

namespace Pqsql
{
	public sealed class PqsqlCopyFrom
	{
		// connection for COPY FROM
		private PqsqlConnection mConn;

		// fixed-size column buffer, flushes content to db connection once page size is reached
		private IntPtr mColBuf;

		// variable-size binary value buffer, used to store encoded column values
		private IntPtr mExpBuf;
		// number of columns in the destination table
		private int mColumns;
		// column datatype information for type inference
		private PqsqlColInfo[] mRowInfo;
		// field position in the current row
		private int mPos;

		public int CopyTimeout { get; set; }

		public string ColumnList { get; set; }

		public string Table { get; set; }


		public PqsqlCopyFrom(PqsqlConnection conn)
		{
			mConn = conn;
			mExpBuf = PqsqlWrapper.createPQExpBuffer();
			mColBuf = IntPtr.Zero;

			mPos = 0;
			mRowInfo = null;
			mColumns = 0;
		}

		~PqsqlCopyFrom()
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

		public void Start()
		{
			if (string.IsNullOrEmpty(Table))
				throw new ArgumentNullException("Table property is null");

			Contract.EndContractBlock();

			// PQexec does not set field types in PQresult for COPY FROM statements,
			// just retrieve 0 rows for the field types of Table

			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("SELECT {0} FROM {1} LIMIT 0", (object) ColumnList ?? '*' , Table);

			// fetch number of columns and store column information
			using
			(
				PqsqlCommand cmd = new PqsqlCommand(mConn)
				{
					CommandText = sb.ToString(), 
					CommandType = CommandType.Text,
					CommandTimeout = CopyTimeout
				}
			)
			using
			(
				PqsqlDataReader r = cmd.ExecuteReader(CommandBehavior.Default)
			)
			{
				// just pick current row information
				PqsqlColInfo[] src = r.RowInformation;
				mColumns = src.Length;
				mRowInfo = new PqsqlColInfo[mColumns];

				Array.Copy(src, mRowInfo, mColumns);
			}

			// reset current field position
			mPos = 0;

			// now build COPY FROM statement
			sb.Clear();
			sb.AppendFormat("COPY {0} (", Table);

			// always create list of columns
			if (string.IsNullOrEmpty(ColumnList))
			{
				bool addsep = false;
				// just assume that we use standard table order
				foreach (PqsqlColInfo row in mRowInfo)
				{
					if (addsep) sb.Append(',');
					addsep = true;
					sb.Append(row.ColumnName);
				}
			}
			else
			{
				// let user decide the column order
				sb.Append(ColumnList);
			}

			sb.Append(") FROM STDIN BINARY");

			byte[] q = PqsqlUTF8Statement.CreateUTF8Statement(sb);
			IntPtr conn = mConn.PGConnection;
			IntPtr res;
			unsafe
			{
				fixed (byte* qp = q)
				{
					res = PqsqlWrapper.PQexec(conn, qp);
				}
			}

			if (res != IntPtr.Zero) // result buffer should contain column information
			{
				ExecStatus s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);

				if (s != ExecStatus.PGRES_COPY_IN)
				{
					PqsqlWrapper.PQclear(res);
					// consume all remaining results until we reach the NULL result
					while ((res = PqsqlWrapper.PQgetResult(conn)) != IntPtr.Zero)
					{
						// always free mResult
						PqsqlWrapper.PQclear(res);
					}

					goto bailout;
				}

				// check first column format, current implementation will have all columns set to binary 
				if (PqsqlWrapper.PQfformat(res, 0) == 0)
				{
					PqsqlWrapper.PQclear(res);
					throw new PqsqlException("PqsqlCopyFrom only supports COPY FROM STDIN BINARY");
				}

				if (mColumns != PqsqlWrapper.PQnfields(res))
				{
					PqsqlWrapper.PQclear(res);
					throw new PqsqlException("Received wrong number of columns for " + sb);
				}

				// done with result inspection
				PqsqlWrapper.PQclear(res);

				if (mColBuf != IntPtr.Zero)
				{
					PqsqlBinaryFormat.pqcb_free(mColBuf);
				}

				mColBuf = PqsqlBinaryFormat.pqcb_create(conn, mColumns);

				return;
			}

			bailout:
			string err = mConn.GetErrorMessage();
			throw new PqsqlException("Could not execute statement «" + sb + "»: " + err);
		}


		public void End()
		{
			IntPtr res;
			string err = string.Empty;
			IntPtr conn = mConn.PGConnection;

			if (mColBuf == IntPtr.Zero)
				return;

			int ret = PqsqlBinaryFormat.pqcb_put_end(mColBuf); // flush column buffer

			if (ret != 1)
			{
				err = err.Insert(0, "Could not send end-of-data indication: ");
				goto bailout;
			}

			res = PqsqlWrapper.PQgetResult(conn);

			if (res != IntPtr.Zero)
			{
				ExecStatus s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);

				PqsqlWrapper.PQclear(res);

				if (s == ExecStatus.PGRES_COPY_IN)
				{
					// still in COPY_IN mode? bail out!
					byte[] b = PqsqlUTF8Statement.CreateUTF8Statement("COPY FROM cancelled by client");

					unsafe
					{
						fixed (byte* bs = b)
						{
							ret = PqsqlWrapper.PQputCopyEnd(conn, bs);
						}
					}

					res = PqsqlWrapper.PQgetResult(conn);

					if (res != IntPtr.Zero)
					{
						s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);
						PqsqlWrapper.PQclear(res);
					}

					err = err.Insert(0, "Cancelled COPY FROM while still in COPY_IN mode (" + s + "," + ret + "): ");

					goto bailout;
				}
				
				if (s != ExecStatus.PGRES_COMMAND_OK)
				{
					err = err.Insert(0, "COPY FROM failed (" + s + "): ");

					goto bailout;
				}

				// consume all remaining results until we reach the NULL result
				while ((res = PqsqlWrapper.PQgetResult(conn)) != IntPtr.Zero)
				{
					// always free mResult
					PqsqlWrapper.PQclear(res);
				}

				return;
			}

		bailout:
			err += mConn.GetErrorMessage();
			throw new PqsqlException(err);
		}


		public void Close()
		{
			if (mExpBuf != IntPtr.Zero)
			{
				PqsqlWrapper.destroyPQExpBuffer(mExpBuf);
				mExpBuf = IntPtr.Zero;
			}

			if (mColBuf != IntPtr.Zero)
			{
				PqsqlBinaryFormat.pqcb_free(mColBuf);
				mColBuf = IntPtr.Zero;
			}
		}


		private string Error()
		{
			IntPtr res;
			string err = string.Empty;
			IntPtr conn = mConn.PGConnection;

			if (mColBuf == IntPtr.Zero)
				return err;

			res = PqsqlWrapper.PQgetResult(conn);

			if (res != IntPtr.Zero)
			{
				ExecStatus s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);

				PqsqlWrapper.PQclear(res);

				if (s == ExecStatus.PGRES_COPY_IN)
				{
					// still in COPY_IN mode? bail out!
					byte[] b = PqsqlUTF8Statement.CreateUTF8Statement("COPY FROM cancelled by client");

					unsafe
					{
						fixed (byte* bs = b)
						{
							PqsqlWrapper.PQputCopyEnd(conn, bs);
						}
					}

					res = PqsqlWrapper.PQgetResult(conn);

					if (res != IntPtr.Zero)
					{
						s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);
						PqsqlWrapper.PQclear(res);
					}
				}

				if (s != ExecStatus.PGRES_COMMAND_OK)
				{
					err = err.Insert(0, "COPY FROM failed (" + s + "): ");

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

		// returns current position of mExpBuf
		private long LengthCheckReset()
		{
			long len = PqsqlBinaryFormat.pqbf_get_buflen(mExpBuf);

			if (len > 4096)
			{
				// if we exceed 4k write-boundary, we reset the buffer and
				// start to write from the beginning again
				PqsqlWrapper.resetPQExpBuffer(mExpBuf);
				len = 0;
			}

			return len;
		}

		// NULL value: value = null, type_length > 0
		// otherwise, value points to beginning of binary encoding with type_length bytes
		private unsafe int PutColumn(sbyte* value, uint type_length)
		{
			int ret = PqsqlBinaryFormat.pqcb_put_col(mColBuf, value, type_length);

			if (ret < 1)
			{
				string err = Error();
				throw new PqsqlException(err);
			}

			// rewind mPos in case we hit column boundary
			if (++mPos >= mColumns)
			{
				mPos = 0;
			}

			return (int) type_length;
		}

		public int WriteNull()
		{
			LengthCheckReset();
			unsafe
			{
				return PutColumn(null, 4); // NULL values have non-zero "length" 
			}
		}

		public int WriteBool(bool b)
		{
			long begin = LengthCheckReset();
			PqsqlBinaryFormat.pqbf_set_bool(mExpBuf, b ? 1 : 0);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 1);
			}
		}

		public int WriteInt2(short i)
		{
			long begin = LengthCheckReset();

			PqsqlColInfo ci = mRowInfo[mPos];
			PqsqlDbType oid = ci.Oid;
			uint destination_length;

			// check destination row datatype
			switch (oid)
			{
				case PqsqlDbType.Int8:
					PqsqlBinaryFormat.pqbf_set_int8(mExpBuf, i);
					destination_length = 8;
					break;
				case PqsqlDbType.Int4:
					PqsqlBinaryFormat.pqbf_set_int4(mExpBuf, i);
					destination_length = 4;
					break;
				case PqsqlDbType.Int2:
					PqsqlBinaryFormat.pqbf_set_int2(mExpBuf, i);
					destination_length = 2;
					break;
				default:
					throw new PqsqlException("Column " + ci.ColumnName + ": cannot write " + typeof(short) + " to column of type " + oid);
			}

			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, destination_length);
			}
		}

		public int WriteInt4(int i)
		{
			long begin = LengthCheckReset();

			PqsqlColInfo ci = mRowInfo[mPos];
			PqsqlDbType oid = ci.Oid;
			uint destination_length;

			// check destination row datatype
			switch (oid)
			{
			case PqsqlDbType.Int8:
				PqsqlBinaryFormat.pqbf_set_int8(mExpBuf, i);
				destination_length = 8;
				break;
			case PqsqlDbType.Int4:
				PqsqlBinaryFormat.pqbf_set_int4(mExpBuf, i);
				destination_length = 4;
				break;
			case PqsqlDbType.Int2:
				// dangerous, but let's try it
				PqsqlBinaryFormat.pqbf_set_int2(mExpBuf, (short) i);
				destination_length = 2;
				break;
			default:
				throw new PqsqlException("Column " + ci.ColumnName + ": cannot write " + typeof(int) + " to column of type " + oid);
			}

			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, destination_length);
			}
		}

		public int WriteInt8(long i)
		{
			long begin = LengthCheckReset();

			PqsqlColInfo ci = mRowInfo[mPos];
			PqsqlDbType oid = ci.Oid;
			uint destination_length;

			// check destination row datatype
			switch (oid)
			{
				case PqsqlDbType.Int8:
					PqsqlBinaryFormat.pqbf_set_int8(mExpBuf, i);
					destination_length = 8;
					break;
				case PqsqlDbType.Int4:
					// dangerous, but let's try it
					PqsqlBinaryFormat.pqbf_set_int4(mExpBuf, (int) i);
					destination_length = 4;
					break;
				case PqsqlDbType.Int2:
					// dangerous, but let's try it
					PqsqlBinaryFormat.pqbf_set_int2(mExpBuf, (short) i);
					destination_length = 2;
					break;
				default:
					throw new PqsqlException("Column " + ci.ColumnName + ": cannot write " + typeof(long) + " to column of type " + oid);
			}

			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, destination_length);
			}
		}

		public int WriteFloat4(float f)
		{
			long begin = LengthCheckReset();

			// TODO try to infer destination datatype
			PqsqlBinaryFormat.pqbf_set_float4(mExpBuf, f);

			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 4);
			}
		}

		public int WriteFloat8(double d)
		{
			long begin = LengthCheckReset();

			// TODO try to infer destination datatype
			PqsqlBinaryFormat.pqbf_set_float8(mExpBuf, d);

			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 8);
			}
		}

		public int WriteNumeric(decimal d)
		{
			long begin = LengthCheckReset();

			// TODO try to infer destination datatype
			PqsqlBinaryFormat.pqbf_set_numeric(mExpBuf, decimal.ToDouble(d));
			long end = PqsqlBinaryFormat.pqbf_get_buflen(mExpBuf);

			long len = end - begin;
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, (uint) len);
			}
		}

		public int WriteText(string s)
		{
			long begin = LengthCheckReset();
			unsafe
			{
				fixed (char* sp = s)
				{
					PqsqlBinaryFormat.pqbf_set_unicode_text(mExpBuf, sp);
				}
				long end = PqsqlBinaryFormat.pqbf_get_buflen(mExpBuf);
				long len = end - begin;

				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, (uint) len);
			}
		}

		public int WriteTimestamp(DateTime dt)
		{
			long begin = LengthCheckReset();

			long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
			long sec = ticks / TimeSpan.TicksPerSecond;
			int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);

			PqsqlBinaryFormat.pqbf_set_timestamp(mExpBuf, sec, usec);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 8);
			}
		}

		public int WriteTime(DateTime dt)
		{
			throw new NotImplementedException("WriteTime not implemented");
		}

		public int WriteDate(DateTime dt)
		{
			throw new NotImplementedException("WriteDate not implemented");
		}

		public int WriteInterval(TimeSpan ts)
		{
			long begin = LengthCheckReset();

			long total_ticks = ts.Ticks;
			int total_days = ts.Days;
			int month = (int) 365.25 * total_days / 12;
			int num_days_month = (int) (12 * month / 365.25);
			int day = total_days - num_days_month;
			long day_month_ticks = day * TimeSpan.TicksPerDay + num_days_month * TimeSpan.TicksPerDay;
			long offset = (total_ticks - day_month_ticks) / TimeSpan.TicksPerDay;

			PqsqlBinaryFormat.pqbf_set_interval(mExpBuf, offset, day, month);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 16);
			}
		}
	}
}
