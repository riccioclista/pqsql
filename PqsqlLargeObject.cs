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


		~PqsqlLargeObject()
		{
			Dispose(false);
		}

		#region Dispose

		public new void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool mDisposed;

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream"/> and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			if (mDisposed)
			{
				return;
			}

			if (disposing)
			{
				// do not close connection
			}

			// always close LO
			Close();

			base.Dispose(disposing);
			mDisposed = true;
		}

		#endregion



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


		internal string GetErrorMessage()
		{
			string msg = string.Empty;

			if (mConn != IntPtr.Zero)
			{
				unsafe
				{
					IntPtr err = new IntPtr(PqsqlWrapper.PQerrorMessage(mConn));

					if (err != IntPtr.Zero)
					{
						msg = PqsqlUTF8Statement.CreateStringFromUTF8(err);
					}
				}
			}

			return msg;
		}


		public override void Close()
		{
			if (mFd < 0 || mConn == IntPtr.Zero)
				return;

			PqsqlWrapper.lo_close(mConn, mFd);
			mFd = -1;
			mMode = 0;
			mPos = -1;
		}

		public override void Flush()
		{
			// noop
		}

		// sets new position in the LO
		public override long Seek(long offset, SeekOrigin whence)
		{
			if (mFd < 0 || mConn == IntPtr.Zero)
				return -1;

			mPos = PqsqlWrapper.lo_lseek64(mConn, mFd, offset, (int) whence);

			return mPos;
		}

		// truncates LO to value
		public override void SetLength(long value)
		{
			if (mFd < 0 || mConn == IntPtr.Zero)
				return;

			int ret = PqsqlWrapper.lo_truncate64(mConn, mFd, value);

			if (ret < 0)
			{
				throw new PqsqlException("Could not truncate large object to " + value + " bytes: " + GetErrorMessage());
			}
		}

		// read from LO
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");

			if (!CanRead)
				throw new NotSupportedException("Reading from large object is turned off");

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

		// write to LO
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");

			if (!CanWrite)
				throw new NotSupportedException("Writing to large object is turned off");

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
				throw new PqsqlException("Could not write to large object: " + GetErrorMessage());
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

				long cur = mPos;
				long ret = Seek(0, SeekOrigin.End);
				if (ret != cur)
					Seek(cur, SeekOrigin.Begin);
				return ret;
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

				Seek(value, SeekOrigin.Begin);
			} 
		}

	}
}
