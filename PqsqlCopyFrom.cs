using System;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

using PqsqlWrapper = Pqsql.UnsafeNativeMethods.PqsqlWrapper;
using PqsqlBinaryFormat = Pqsql.UnsafeNativeMethods.PqsqlBinaryFormat;

namespace Pqsql
{
	public sealed class PqsqlCopyFrom : PqsqlCopyBase
	{
		// fixed-size column buffer, flushes content to db connection once page size is reached
		private IntPtr mColBuf;

		// variable-size binary value buffer, used to store encoded column values
		private IntPtr mExpBuf;

		// field position in the current row
		private int mPos;

		protected override string CopyStmtDirection { get; } = "FROM STDIN";

        internal override ExecStatusType QueryResultType { get; } = ExecStatusType.PGRES_COPY_IN;
        
		public PqsqlCopyFrom(PqsqlConnection conn)
			: base(conn)
		{
			mColBuf = IntPtr.Zero;
			mExpBuf = PqsqlWrapper.createPQExpBuffer();
			mPos = 0;
		}

		public override void Start()
		{
			base.Start();

			if (mColBuf != IntPtr.Zero)
			{
				PqsqlBinaryFormat.pqcb_free(mColBuf);
			}

			IntPtr conn = mConn.PGConnection;
			mColBuf = PqsqlBinaryFormat.pqcb_create(conn, mColumns);
		}

        public override void Close()
        {
			if (mColBuf != IntPtr.Zero)
			{
				PqsqlBinaryFormat.pqcb_free(mColBuf);
				mColBuf = IntPtr.Zero;
			}
			
			if (mExpBuf != IntPtr.Zero)
			{
				PqsqlWrapper.destroyPQExpBuffer(mExpBuf);
				mExpBuf = IntPtr.Zero;
			}
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
				string err = mColBuf == IntPtr.Zero ? string.Empty : Error();
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
#if !WIN32
				PqsqlUTF8Statement.SetText(mExpBuf, value);
#else
				fixed (char* sp = value)
				{
					PqsqlBinaryFormat.pqbf_set_unicode_text(mExpBuf, sp);
				}
#endif
				long end = PqsqlBinaryFormat.pqbf_get_buflen(mExpBuf);
				long len = end - begin;

				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, (uint) len);
			}
		}

		public int WriteTimestamp(DateTime value)
		{
			long begin = LengthCheckReset();

			long sec;
			int usec;
			PqsqlBinaryFormat.GetTimestamp(value, out sec, out usec);

			PqsqlBinaryFormat.pqbf_set_timestamp(mExpBuf, sec, usec);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 8);
			}
		}

		public int WriteTime(TimeSpan value)
		{
			long begin = LengthCheckReset();

			int hour;
			int min;
			int sec;
			int fsec;
			PqsqlBinaryFormat.GetTime(value, out hour, out min, out sec, out fsec);

			PqsqlBinaryFormat.pqbf_set_time(mExpBuf, hour, min, sec, fsec);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 8);
			}
		}

		public int WriteTimeTZ(TimeSpan value)
		{
			long begin = LengthCheckReset();

			int hour;
			int min;
			int sec;
			int fsec;
			int tz;
			PqsqlBinaryFormat.GetTimeTZ(value, out hour, out min, out sec, out fsec, out tz);

			PqsqlBinaryFormat.pqbf_set_timetz(mExpBuf, hour, min, sec, fsec, tz);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 12);
			}
		}

		public int WriteDate(DateTime value)
		{
			if (mRowInfo == null)
			{
				throw new InvalidOperationException($"{nameof(PqsqlCopyFrom)}.{nameof(Start)} must be called before we can write data");
			}

			long begin = LengthCheckReset();

#if CODECONTRACTS
			Contract.Assume(mRowInfo != null);
			Contract.Assume(mPos >= 0 && mPos < mRowInfo.Length);
#endif

			PqsqlColInfo ci = mRowInfo[mPos];
			if (ci == null)
			{
				throw new PqsqlException($"{nameof(PqsqlCopyFrom)}.{nameof(Start)} could not setup column information for column {mPos}");
			}

			PqsqlDbType oid = ci.Oid;

			if (oid != PqsqlDbType.Date)
			{
				throw new PqsqlException($"{nameof(PqsqlCopyFrom)}.{nameof(WriteDate)}: cannot write {PqsqlDbType.Date} into column {mPos} of type {oid}");
			}

			int year;
			int month;
			int day;
			PqsqlBinaryFormat.GetDate(value, out year, out month, out day);

			PqsqlBinaryFormat.pqbf_set_date(mExpBuf, year, month, day);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 4);
			}
		}

		public int WriteInterval(TimeSpan value)
		{
			long begin = LengthCheckReset();

			long offset;
			int day;
			int month;
			PqsqlBinaryFormat.GetInterval(value, out offset, out day, out month);

			PqsqlBinaryFormat.pqbf_set_interval(mExpBuf, offset, day, month);
			unsafe
			{
				sbyte* val = PqsqlBinaryFormat.pqbf_get_bufval(mExpBuf) + begin;
				return PutColumn(val, 16);
			}
		}

		public int WriteArray(Array value)
		{
			throw new NotImplementedException("WriteArray not implemented");
		}
	}
}
