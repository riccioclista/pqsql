using System;
using System.Runtime.InteropServices;
using System.Text;
using PqsqlWrapper = Pqsql.UnsafeNativeMethods.PqsqlWrapper;
using PqsqlBinaryFormat = Pqsql.UnsafeNativeMethods.PqsqlBinaryFormat;

namespace Pqsql
{
	public unsafe sealed class PqsqlCopyTo : PqsqlCopyBase
	{
		// PGCOPY\n\377\r\n\0
		private static readonly sbyte[] BinHeader = { 80, 71, 67, 79, 80, 89, 10, -1, 13, 10, 0 };

		private readonly IntPtr mPGConn;

		private IntPtr mBufferPtr;
		private IntPtr mReadPos;
		private bool mHeaderReceived;
		private IntPtr mRowStart;

		public PqsqlCopyTo(PqsqlConnection conn)
			: base(conn)
		{
			if (conn == null)
			{
				throw new ArgumentNullException(nameof(conn));
			}

			mPGConn = conn.PGConnection;
			mBufferPtr = Marshal.AllocHGlobal(sizeof(IntPtr));
			*((IntPtr*)mBufferPtr) = IntPtr.Zero;
		}
		
		public string Query
		{
			get => QueryInternal;
			set => QueryInternal = value?.Trim().TrimEnd(';');
		}

		public bool SuppressSchemaQuery
		{
			get => SuppressSchemaQueryInternal;
			set => SuppressSchemaQueryInternal = value;
		}

		protected override string CopyStmtDirection { get; } = "TO STDOUT";

        internal override ExecStatusType QueryResultType { get; } = ExecStatusType.PGRES_COPY_OUT;

		private IntPtr Buffer => *((IntPtr*) mBufferPtr);

        public override void Close()
        {
			if (mBufferPtr == IntPtr.Zero)
			{
				return;
			}

			var buffer = Buffer;
			if (buffer != IntPtr.Zero)
			{
				PqsqlWrapper.PQfreemem(buffer);
			}
			
			Marshal.FreeHGlobal(mBufferPtr);
			mBufferPtr = IntPtr.Zero;
        }

		public bool FetchRow()
		{
			var res = FetchRowCore();
			mReadPos = Buffer;

			if (!mHeaderReceived)
			{
				if (res == -1)
				{
					throw new PqsqlException("Unexpected EOF curing COPY header retrieval.");
				}

				// read the binary format header: https://www.postgresql.org/docs/current/sql-copy.html
				// 11 byte signature
				for (int i = 0; i < 11; i++)
				{
					var val = (sbyte*) mReadPos;
					if (*val != BinHeader[i])
					{
						throw new PqsqlException("COPY binary format header not readable.");
					}

					mReadPos += 1;
				}

				// bytes 11 to 14 (zero-based counting) are a 32 bit long mask, with bit 16 being the only one used
				var bitMask = PqsqlUtils.ReadInt32(ref mReadPos);
				var hasOids = (bitMask & (1 << 16)) > 0;
				if (hasOids)
				{
					throw new NotSupportedException("Pqsql doesn't support COPY TO operations with column OIDs in result tuples");
				}

				// the rest (from byte 15 onwards) is a variable length header that we can ignore
				var extHeaderSize = PqsqlUtils.ReadInt32(ref mReadPos);
				mReadPos += extHeaderSize;
				mHeaderReceived = true;

				// now we can continue to the first tuple, which follows immediately
			}
			
			// remember where row starts (in case of reader error)
			mRowStart = mReadPos;

			if (res > 0)
			{
				// read 16-bit integer with field count
				var fieldCount = PqsqlUtils.ReadInt16(ref mReadPos);
				if (fieldCount == mColumns)
				{
					// got a tuple, continue
					return true;
				}

				if (fieldCount == -1)
				{
					// we received the end trailer, read again (we expect -1) and return false
					var lastReadResult = FetchRowCore();
					if (lastReadResult != -1)
					{
						throw new PqsqlException($"The read after the trailer didn't return -1, but '{lastReadResult}'.");
					}

					// get the final result from the conn, so we can return to normal operation
					if (mPGConn == IntPtr.Zero)
					{
						throw new InvalidOperationException("Connection is closed.");
					}

					IntPtr result = PqsqlWrapper.PQgetResult(mPGConn);
					if (result != IntPtr.Zero)
					{
						var s = PqsqlWrapper.PQresultStatus(result);
						PqsqlWrapper.PQclear(result);

						if (s == ExecStatusType.PGRES_COMMAND_OK)
						{
							// consume all remaining results until we reach the NULL result
							while ((result = PqsqlWrapper.PQgetResult(mPGConn)) != IntPtr.Zero)
							{
								PqsqlWrapper.PQclear(result);
							}

							return false;
						}

						throw new PqsqlException($"COPY failed with status '{s}'.");
					}
					
					throw new PqsqlException($"COPY failed with zero status code.");
				}

				// we got an invalid fieldCount
				throw new PqsqlException($"The COPY operation received an invalid field count of '{fieldCount}'.");
			}
			
			if (res == 0)
			{
				throw new PqsqlException("Data not yet available. This can only happen in async mode.");
			}

			if (res == -1)
			{
				// EOF
				return false;
			}

			// res < -1
			var err = Error();
			throw new PqsqlException(err);
		}

		private int FetchRowCore()
		{
			if (mPGConn == IntPtr.Zero)
			{
				throw new InvalidOperationException("Connection is closed.");
			}

			var buffer = Buffer;
			if (buffer != IntPtr.Zero)
			{
				PqsqlWrapper.PQfreemem(buffer);
			}

			return PqsqlWrapper.PQgetCopyData(mPGConn, mBufferPtr, 0);
		}

		/// <summary>
		/// Determines whether the next value is <c>null</c>. If it is <c>null</c> the
		/// read cursor is advanced to the next value. Otherwise the read cursor stays put.
		/// </summary>
		public bool IsNull()
		{
			var p = (int*) mReadPos;
			var size = PqsqlUtils.SwapBytes(*p);
			if (size == -1)
			{
				mReadPos += sizeof(int);
				return true;
			}

			// if current value is not null, don't advance the cursor so the client can read the value
			return false;
		}

		public bool ReadBoolean()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return false;
			}

			if (size != 1)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadBoolean));
			}

			return PqsqlUtils.ReadBool(ref mReadPos);
		}

		public short ReadInt2()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return 0;
			}

			if (size != 2)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadInt2));
			}

			return PqsqlUtils.ReadInt16(ref mReadPos);
		}

		public int ReadInt4()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return 0;
			}

			if (size != 4)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadInt4));
			}

			return PqsqlUtils.ReadInt32(ref mReadPos);
		}

		public long ReadInt8()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return 0;
			}

			if (size != 8)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadInt8));
			}

			return PqsqlUtils.ReadInt64(ref mReadPos);
		}

		public float ReadFloat4()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return 0;
			}

			if (size != 4)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadFloat4));
			}

			return PqsqlUtils.ReadFloat32(ref mReadPos);
		}

		public double ReadFloat8()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return 0;
			}

			if (size != 8)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadFloat8));
			}

			return PqsqlUtils.ReadFloat64(ref mReadPos);
		}

		public string ReadString()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return null;
			}

			var p = (byte*) mReadPos;
			var text = Encoding.UTF8.GetString(p, size);
			mReadPos += size;
			return text;
		}

		public DateTime ReadTimestamp()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return DateTime.MinValue;
			}

			if (size != 8)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadTimestamp));
			}

			var ts = PqsqlUtils.ReadInt64(ref mReadPos);
			return PqsqlBinaryFormat.GetDateTimeFromTimestamp(ts);
		}

		public DateTime ReadTime()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return DateTime.MinValue;
			}

			if (size != 8)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadTime));
			}

			var t = PqsqlUtils.ReadInt64(ref mReadPos);
			return PqsqlBinaryFormat.GetDateTimeFromTime(t);	
		}

		public DateTimeOffset ReadTimeTZ()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return DateTimeOffset.MinValue;
			}

			if (size != 12)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadTimeTZ));
			}

			var t = PqsqlUtils.ReadInt64(ref mReadPos);
			var tz = PqsqlUtils.ReadInt32(ref mReadPos);
			return PqsqlBinaryFormat.GetDateTimeOffsetFromTime(t, tz);
		}

		public DateTime ReadDate()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return DateTime.MinValue;
			}

			if (size != 4)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadDate));
			}
			
			var jDate = PqsqlUtils.ReadInt32(ref mReadPos);
			return PqsqlBinaryFormat.GetDateTimeFromJDate(jDate);
		}

		public TimeSpan ReadInterval()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return TimeSpan.MinValue;
			}

			if (size != 16)
			{
				ThrowInvalidRead(nameof(PqsqlCopyTo.ReadInterval));
			}

			var offset = PqsqlUtils.ReadInt64(ref mReadPos);
			var day = PqsqlUtils.ReadInt32(ref mReadPos);
			var month = PqsqlUtils.ReadInt32(ref mReadPos);
			return PqsqlBinaryFormat.GetTimeSpan(offset, day, month);
		}

		public byte[] ReadRaw()
		{
			var size = PqsqlUtils.ReadInt32(ref mReadPos);
			if (size == -1)
			{
				return null;
			}

			var result = new byte[size];
			Marshal.Copy(mReadPos, result, 0, size);
			mReadPos += size;
			return result;
		}

		private void ThrowInvalidRead(string currentReadMethodName)
		{
			// read again to find out at which col we are; start at the beginning of the row (skip the field count)
			var p = mRowStart + sizeof(short);

			// end at beginn of current tuple (we've already read the size, so unwind these 4 bytes)
			mReadPos -= sizeof(int);

			int col = 0;
			while (p != mReadPos)
			{
				var size = PqsqlUtils.ReadInt32(ref p);
				p += size;
				col++;
			}

			string errMsg;
			if (mRowInfo != null)
			{
				var info = mRowInfo[col];
				errMsg = $"The type of the current column '{info.ColumnName}' is '{info.Oid}'. " +
						 $"You cannot read an '{info.Oid}' value using '{currentReadMethodName}'.";
			}
			else
			{
				var size = PqsqlUtils.ReadInt32(ref p);
				errMsg = $"The type of the current column at position {col} with size {size} " +
						 $"cannot be read using '{currentReadMethodName}'.";
			}

			throw new InvalidOperationException(errMsg);
		}
	}
}
