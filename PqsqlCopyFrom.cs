using System;
using System.Text;

namespace Pqsql
{
	public class PqsqlCopyFrom
	{

		private PqsqlConnection mConn;

		// fixed-size column buffer, flushes content to db connection once page size is reached
		private IntPtr mColBuf;

		// variable-size binary value buffer, used to store encoded column values
		private IntPtr mExpBuf;

		public PqsqlCopyFrom(PqsqlConnection conn)
		{
			mConn = conn;
			mExpBuf = PqsqlWrapper.createPQExpBuffer();
			mColBuf = IntPtr.Zero;
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

		protected void Dispose(bool disposing)
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

		public void Start(string copy_query)
		{
			IntPtr res;
			byte[] q = PqsqlProviderFactory.Instance.CreateUTF8Statement(copy_query);
			IntPtr conn = mConn.PGConnection;

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
					throw new PqsqlException("PqsqlCopyFrom only supports COPY FROM STDIN BINARY");
				}

				// get number of columns
				int num_cols = PqsqlWrapper.PQnfields(res);
				
				// done with result inspection
				PqsqlWrapper.PQclear(res);

				if (mColBuf != IntPtr.Zero)
				{
					PqsqlBinaryFormat.pqcb_free(mColBuf);
				}

				mColBuf = PqsqlBinaryFormat.pqcb_create(conn, num_cols);

				return;
			}

			bailout:
			string err = mConn.GetErrorMessage();
			throw new PqsqlException(err);
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
				err = err.Insert(0, "END failed: ");
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
					byte[] b = PqsqlProviderFactory.Instance.CreateUTF8Statement("COPY FROM cancelled by client");

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

					err = err.Insert(0, "COPY FROM failed(" + s + "," + ret + "): ");

					goto bailout;
				}
				
				if (s != ExecStatus.PGRES_COMMAND_OK)
				{
					err = err.Insert(0, "COPY FROM failed(" + s + "): ");

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
					byte[] b = PqsqlProviderFactory.Instance.CreateUTF8Statement("COPY FROM cancelled by client");

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
					err = err.Insert(0, "COPY FROM failed(" + s + "): ");

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

		public int WriteInt2(short i)
		{
			long begin = LengthCheckReset();
			PqsqlBinaryFormat.pqbf_set_int2(mExpBuf, i);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 2);
			}
		}

		public int WriteInt4(int i)
		{
			long begin = LengthCheckReset();
			PqsqlBinaryFormat.pqbf_set_int4(mExpBuf, i);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 4);
			}
		}

		public int WriteInt8(long i)
		{
			long begin = LengthCheckReset();
			PqsqlBinaryFormat.pqbf_set_int8(mExpBuf, i);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 8);
			}
		}

		public int WriteFloat4(float f)
		{
			long begin = LengthCheckReset();
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
			int usec = (int) ((ticks % TimeSpan.TicksPerSecond) / 10);

			PqsqlBinaryFormat.pqbf_set_timestamp(mExpBuf, sec, usec);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 8);
			}
		}

		public unsafe int WriteTime(DateTime dt)
		{
			throw new NotImplementedException();
		}

		public unsafe int WriteDate(DateTime dt)
		{
			throw new NotImplementedException();
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
