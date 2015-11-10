using System;
using System.IO;

namespace Pqsql
{
	public sealed class PqsqlLargeObject : Stream
	{

		private readonly IntPtr mConn;

		private int mFd;

		private LoOpen mMode;

		private long mPos;

		public PqsqlLargeObject(PqsqlConnection conn)
		{
			if (conn == null)
			{
				throw new ArgumentNullException("conn");
			}
			mConn = conn.PGConnection;
			mFd = -1;
			mMode = 0;
			mPos = -1;
		}

		public uint Create()
		{
			return PqsqlWrapper.lo_create(mConn, 0);
		}

		public int Unlink(uint oid)
		{
			return PqsqlWrapper.lo_unlink(mConn, oid);
		}

		public int Open(uint oid, LoOpen mode)
		{
			mFd = PqsqlWrapper.lo_open(mConn, oid, (int) mode);
			mMode = mode;
			mPos = 0;
			return mFd;
		}




		public override void Close()
		{
			if (mFd < 0 || mConn == IntPtr.Zero)
				return;

			int ret = PqsqlWrapper.lo_close(mConn, mFd);
			mFd = -1;
			mMode = 0;
			mPos = -1;

			if (ret < 0)
			{
				throw new PqsqlException("lo_close failed: " + ret);
			}
		}

		public override void Flush()
		{
		}


		public override long Seek(long offset, SeekOrigin whence)
		{
			if (mFd < 0 || mConn == IntPtr.Zero)
				return -1;

			return PqsqlWrapper.lo_lseek64(mConn, mFd, offset, (int) whence);
		}

		public override void SetLength(long value)
		{
			if (mFd < 0 || mConn == IntPtr.Zero)
				return;

			int len = PqsqlWrapper.lo_truncate64(mConn, mFd, value);

			if (len < value)
			{
				throw new PqsqlException("lo_truncate64 failed: " + len);
			}

			if (mPos > len)
				mPos = len;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");

			if (!CanRead)
				throw new NotSupportedException("Cannot read from large object");

			int blen = buffer.Length;

			if (offset + count > blen)
				throw new ArgumentException("offset or count larger than buffer");
			
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");

			if (count < 0)
				throw new ArgumentOutOfRangeException("count");

			unsafe
			{
				fixed (byte* b = &buffer[offset])
				{
					int read = PqsqlWrapper.lo_read(mConn, mFd, b, (ulong) count);
					mPos += read;
					return read;
				}
			}
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");

			if (!CanWrite)
				throw new NotSupportedException("Cannot write from large object");

			int blen = buffer.Length;

			if (offset + count > blen)
				throw new ArgumentException("offset or count larger than buffer");

			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");

			if (count < 0)
				throw new ArgumentOutOfRangeException("count");

			int ret = 1;

			unsafe
			{
				while (count > 0 && ret > 0)
				{
					fixed (byte* b = &buffer[offset])
					{
						ret = PqsqlWrapper.lo_write(mConn, mFd, b, (ulong) count);
					}

					if (ret > 0)
					{
						offset += ret;
						count -= ret;
						mPos += ret;
					}
				}
			}
	
			if (ret < 0)
			{
				throw new PqsqlException("lo_write failed: " + ret);
			}
		}

		public override bool CanRead
		{
			get
			{
				return (mMode & LoOpen.INV_READ) == LoOpen.INV_READ;
			}
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override bool CanWrite
		{
			get
			{
				return (mMode & LoOpen.INV_WRITE) == LoOpen.INV_WRITE;
			}
		}

		public override long Length
		{
			get
			{
				if (mFd < 0 || mConn == IntPtr.Zero)
					return -1;

				return PqsqlWrapper.lo_tell64(mConn, mFd);
			}
		}

		public override long Position
		{
			get
			{
				if (mFd < 0 || mConn == IntPtr.Zero)
					return -1;

				return mPos;
			}

			set
			{
				if (mFd < 0 || mConn == IntPtr.Zero || mPos == value)
					return;

				mPos = PqsqlWrapper.lo_lseek64(mConn, mFd, value, (int) SeekOrigin.Begin);
			} 
		}

	}
}
