﻿using System;
using System.Data;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif
using System.Text;

using PqsqlWrapper = Pqsql.UnsafeNativeMethods.PqsqlWrapper;
using PqsqlBinaryFormat = Pqsql.UnsafeNativeMethods.PqsqlBinaryFormat;

namespace Pqsql
{
	public sealed class PqsqlCopyFrom : IDisposable
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
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(Table), "Table property is null");
#else
			if (string.IsNullOrEmpty(Table))
				throw new ArgumentNullException("Table property is null");
#endif

			// PQexec does not set field types in PQresult for COPY FROM statements,
			// just retrieve 0 rows for the field types of Table

			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("SELECT {0} FROM {1} LIMIT 0", (object) ColumnList ?? '*' , Table);

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
						throw new PqsqlException("Cannot retrieve RowInformation for table " + Table);

					mColumns = src.Length;
					mRowInfo = new PqsqlColInfo[mColumns];

					Array.Copy(src, mRowInfo, mColumns);
				}
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

			IntPtr res;
			ExecStatusType s = mConn.Exec(q, out res);

			// result buffer should contain column information and PGconn should be in COPY_IN state
			if (res == IntPtr.Zero || s != ExecStatusType.PGRES_COPY_IN)
			{
				mConn.Consume(res); // we might receive several results...
				throw new PqsqlException("Could not execute statement «" + sb + "»: " + mConn.GetErrorMessage());
			}

			// check first column format, current implementation will have all columns set to binary 
			if (PqsqlWrapper.PQfformat(res, 0) == 0)
			{
				mConn.Consume(res);
				throw new PqsqlException("PqsqlCopyFrom only supports COPY FROM STDIN BINARY");
			}

			// sanity check
			if (mColumns != PqsqlWrapper.PQnfields(res))
			{
				mConn.Consume(res);
				throw new PqsqlException("Received wrong number of columns for " + sb);
			}

			// done with result inspection
			PqsqlWrapper.PQclear(res);

			if (mColBuf != IntPtr.Zero)
			{
				PqsqlBinaryFormat.pqcb_free(mColBuf);
			}

			IntPtr conn = mConn.PGConnection;
			mColBuf = PqsqlBinaryFormat.pqcb_create(conn, mColumns);
		}


		public void End()
		{
			IntPtr res;
			string err = string.Empty;

#if CODECONTRACTS
			Contract.Assume(mConn != null);
#endif
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
				ExecStatusType s = PqsqlWrapper.PQresultStatus(res);

				PqsqlWrapper.PQclear(res);

				if (s == ExecStatusType.PGRES_COPY_IN)
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
						s = PqsqlWrapper.PQresultStatus(res);
						PqsqlWrapper.PQclear(res);
					}

					err = err.Insert(0, "Cancelled COPY FROM while still in COPY_IN mode (" + s + "," + ret + "): ");

					goto bailout;
				}
				
				if (s != ExecStatusType.PGRES_COMMAND_OK)
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
				ExecStatusType s = PqsqlWrapper.PQresultStatus(res);

				PqsqlWrapper.PQclear(res);

				if (s == ExecStatusType.PGRES_COPY_IN)
				{
					// still in COPY_IN mode? bail out!
					byte[] b = PqsqlUTF8Statement.CreateUTF8Statement("COPY FROM cancelled by client");
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
						err = err.Insert(0, "Cannot cancel COPY FROM (" + s + "): ");

						goto bailout;
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
#if CODECONTRACTS
			Contract.Ensures(mPos < mColumns);
#endif

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

		public int WriteBool(bool value)
		{
			long begin = LengthCheckReset();
			PqsqlBinaryFormat.pqbf_set_bool(mExpBuf, value ? 1 : 0);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 1);
			}
		}

		public int WriteInt2(short value)
		{
			if (mRowInfo == null)
				throw new InvalidOperationException("PqsqlCopyFrom.Start must be called before we can write data");

			long begin = LengthCheckReset();

#if CODECONTRACTS
			Contract.Assume(mRowInfo != null);
			Contract.Assume(mPos >= 0 && mPos < mRowInfo.Length);
#endif

			PqsqlColInfo ci = mRowInfo[mPos];
			if (ci == null)
				throw new PqsqlException("PqsqlCopyFrom.Start could not setup column information for column " + mPos);

			PqsqlDbType oid = ci.Oid;
			uint destination_length;

			// check destination row datatype
			switch (oid)
			{
				case PqsqlDbType.Int8:
					PqsqlBinaryFormat.pqbf_set_int8(mExpBuf, value);
					destination_length = 8;
					break;
				case PqsqlDbType.Int4:
					PqsqlBinaryFormat.pqbf_set_int4(mExpBuf, value);
					destination_length = 4;
					break;
				case PqsqlDbType.Int2:
					PqsqlBinaryFormat.pqbf_set_int2(mExpBuf, value);
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

		public int WriteInt4(int value)
		{
			if (mRowInfo == null)
				throw new InvalidOperationException("PqsqlCopyFrom.Start must be called before we can write data");

			long begin = LengthCheckReset();

#if CODECONTRACTS
			Contract.Assume(mRowInfo != null);
			Contract.Assume(mPos >= 0 && mPos < mRowInfo.Length);
#endif

			PqsqlColInfo ci = mRowInfo[mPos];
			if (ci == null)
				throw new PqsqlException("PqsqlCopyFrom.Start could not setup column information for column " + mPos);

			PqsqlDbType oid = ci.Oid;
			uint destination_length;

			// check destination row datatype
			switch (oid)
			{
			case PqsqlDbType.Int8:
				PqsqlBinaryFormat.pqbf_set_int8(mExpBuf, value);
				destination_length = 8;
				break;
			case PqsqlDbType.Int4:
				PqsqlBinaryFormat.pqbf_set_int4(mExpBuf, value);
				destination_length = 4;
				break;
			case PqsqlDbType.Int2:
				// dangerous, but let's try it
				PqsqlBinaryFormat.pqbf_set_int2(mExpBuf, (short) value);
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

		public int WriteInt8(long value)
		{
			if (mRowInfo == null)
				throw new InvalidOperationException("PqsqlCopyFrom.Start must be called before we can write data");

			long begin = LengthCheckReset();

#if CODECONTRACTS
			Contract.Assume(mRowInfo != null);
			Contract.Assume(mPos >= 0 && mPos < mRowInfo.Length);
#endif

			PqsqlColInfo ci = mRowInfo[mPos];
			if (ci == null)
				throw new PqsqlException("PqsqlCopyFrom.Start could not setup column information for column " + mPos);

			PqsqlDbType oid = ci.Oid;
			uint destination_length;

			// check destination row datatype
			switch (oid)
			{
				case PqsqlDbType.Int8:
					PqsqlBinaryFormat.pqbf_set_int8(mExpBuf, value);
					destination_length = 8;
					break;
				case PqsqlDbType.Int4:
					// dangerous, but let's try it
					PqsqlBinaryFormat.pqbf_set_int4(mExpBuf, (int) value);
					destination_length = 4;
					break;
				case PqsqlDbType.Int2:
					// dangerous, but let's try it
					PqsqlBinaryFormat.pqbf_set_int2(mExpBuf, (short) value);
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

		public int WriteFloat4(float value)
		{
			long begin = LengthCheckReset();

			// TODO try to infer destination datatype
			PqsqlBinaryFormat.pqbf_set_float4(mExpBuf, value);

			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 4);
			}
		}

		public int WriteFloat8(double value)
		{
			long begin = LengthCheckReset();

			// TODO try to infer destination datatype
			PqsqlBinaryFormat.pqbf_set_float8(mExpBuf, value);

			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 8);
			}
		}

		public int WriteNumeric(decimal value)
		{
			long begin = LengthCheckReset();

			// TODO try to infer destination datatype
			PqsqlBinaryFormat.pqbf_set_numeric(mExpBuf, decimal.ToDouble(value));
			long end = PqsqlBinaryFormat.pqbf_get_buflen(mExpBuf);

			long len = end - begin;
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, (uint) len);
			}
		}

		public int WriteText(string value)
		{
			long begin = LengthCheckReset();
			unsafe
			{
				fixed (char* sp = value)
				{
					PqsqlBinaryFormat.pqbf_set_unicode_text(mExpBuf, sp);
				}
				long end = PqsqlBinaryFormat.pqbf_get_buflen(mExpBuf);
				long len = end - begin;

				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, (uint) len);
			}
		}

		public int WriteTimestamp(DateTime value)
		{
			long begin = LengthCheckReset();

			long ticks = value.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
			long sec = ticks / TimeSpan.TicksPerSecond;
			int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);

			PqsqlBinaryFormat.pqbf_set_timestamp(mExpBuf, sec, usec);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 8);
			}
		}

		public int WriteTime(DateTime value)
		{
			throw new NotImplementedException("WriteTime not implemented");
		}

		public int WriteDate(DateTime value)
		{
			throw new NotImplementedException("WriteDate not implemented");
		}

		public int WriteInterval(TimeSpan value)
		{
			long begin = LengthCheckReset();

			long total_ticks = value.Ticks;
			int total_days = value.Days;
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
