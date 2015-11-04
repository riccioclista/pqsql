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

			// always release mColBuf (must not throw exception)
			Close();

			mDisposed = true;
		}

		#endregion

		public void Start(string copy_query)
		{
			IntPtr res;
			byte[] q = Encoding.UTF8.GetBytes(copy_query);
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

				if (s != ExecStatus.PGRES_COMMAND_OK)
				{
					// not done yet or still in COPY_IN mode? bail out!
					byte[] b = Encoding.UTF8.GetBytes("COPY FROM cancelled by client");

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


		private unsafe sbyte* Top(bool reset)
		{
			sbyte* data = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf);
			long len = PqsqlBinaryFormat.pqbf_get_buflen(mExpBuf);

			if (reset && len > 4096)
			{
				// if we exceed 4k write-boundary, we reset the buffer and
				// start to write from the beginning again
				PqsqlWrapper.resetPQExpBuffer(mExpBuf);
				return data; // start from beginning of PQExpBuffer
			}

			return data + len; // get top of PQExpBuffer
		}


		private unsafe int PutColumn(sbyte* value, uint type_length)
		{
			int ret = PqsqlBinaryFormat.pqcb_put_col(mColBuf, value, type_length);
			return ret == 1 ? (int) type_length : ret;
		}

		public unsafe int WriteInt2(short i)
		{
			sbyte* val = Top(true);
			PqsqlBinaryFormat.pqbf_set_int2(mExpBuf, i);
			return PutColumn(val, 2);
		}

		public unsafe int WriteInt4(int i)
		{
			sbyte* val = Top(true);
			PqsqlBinaryFormat.pqbf_set_int4(mExpBuf, i);
			return PutColumn(val, 4);
		}

		public unsafe int WriteInt8(long i)
		{
			sbyte* val = Top(true);
			PqsqlBinaryFormat.pqbf_set_int8(mExpBuf, i);
			return PutColumn(val, 8);
		}

		public unsafe int WriteFloat4(float f)
		{
			sbyte* val = Top(true);
			PqsqlBinaryFormat.pqbf_set_float4(mExpBuf, f);
			return PutColumn(val, 4);
		}

		public unsafe int WriteFloat8(double d)
		{
			sbyte* val = Top(true);
			PqsqlBinaryFormat.pqbf_set_float8(mExpBuf, d);
			return PutColumn(val, 8);
		}

		public unsafe int WriteNumeric(decimal d)
		{
			sbyte* val = Top(true);
			PqsqlBinaryFormat.pqbf_set_numeric(mExpBuf, decimal.ToDouble(d));
			sbyte* top = Top(false);
			long len = top - val;
			return PutColumn(val, (uint) len);
		}

		public unsafe int WriteText(string s)
		{
			sbyte* val = Top(true);
			fixed (char* sp = s)
			{
				PqsqlBinaryFormat.pqbf_set_unicode_text(mExpBuf, sp);
			}
			sbyte* top = Top(false);
			long len = top - val;
			return PutColumn(val, (uint) len);
		}

		public unsafe int WriteTimestamp(DateTime dt)
		{
			sbyte* val = Top(true);
			long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
			long sec = ticks / TimeSpan.TicksPerSecond;
			int usec = (int) ((ticks % TimeSpan.TicksPerSecond) / 10);
			PqsqlBinaryFormat.pqbf_set_timestamp(mExpBuf, sec, usec);
			return PutColumn(val, 8);
		}

		public unsafe int WriteTime(DateTime dt)
		{
			throw new NotImplementedException();
		}

		public unsafe int WriteDate(DateTime dt)
		{
			throw new NotImplementedException();
		}

		public unsafe int WriteInterval(TimeSpan ts)
		{
			throw new NotImplementedException();
#if false
			sbyte* val = Top();
			long ticks = ts.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
			long sec = ticks / TimeSpan.TicksPerSecond;
			int usec = (int) ((ticks % TimeSpan.TicksPerSecond) / 10);
			PqsqlBinaryFormat.pqbf_set_timestamp(mExpBuf, sec, usec);
			return PutColumn(val, 8);
#endif
		}
	}
}
