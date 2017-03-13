using System;
using System.ComponentModel;
using System.Globalization;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif
using System.IO;

using PqsqlWrapper = Pqsql.UnsafeNativeMethods.PqsqlWrapper;

namespace Pqsql
{
	public sealed class PqsqlLargeObject : Stream
	{
		// reference connection
		private readonly PqsqlConnection mConn;
		// PGConn struct
		private readonly IntPtr mPGConn;

		// the oid of the large object
		private uint mOid;
		// file descriptor for mOid
		private int mFd;
		// open mode
		private LoOpen mMode;
		// current position of mFd
		private long mPos;

		public PqsqlLargeObject(PqsqlConnection conn)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(conn != null);
#else
			if (conn == null)
				throw new ArgumentNullException(nameof(conn));
#endif

			// All large object manipulation using these functions must take place within an SQL transaction block,
			// since large object file descriptors are only valid for the duration of a transaction.
			// https://www.postgresql.org/docs/current/static/lo-interfaces.html
			PGTransactionStatusType transactionStatus = conn.TransactionStatus;
			if (transactionStatus != PGTransactionStatusType.PQTRANS_INTRANS &&
					transactionStatus != PGTransactionStatusType.PQTRANS_ACTIVE)
			{
				throw new PqsqlException("PqsqlLargeObject manipulation must take place within an SQL transaction");
			}

			mConn = conn;
			mPGConn = conn.PGConnection;
			if (mPGConn == IntPtr.Zero)
			{
				throw new ArgumentNullException("PqsqlConnection is closed: " + mConn.GetErrorMessage());
			}

			mOid = 0;
			mFd = -1;
			mMode = 0;
			mPos = -1;
		}


		~PqsqlLargeObject()
		{
			Dispose(false);
		}

		#region Dispose

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
			// returns 0 (InvalidOid) on error
			uint ret = PqsqlWrapper.lo_create(mPGConn, 0);

			if (ret == 0)
			{
				throw new PqsqlException("Could not create large object: " + mConn.GetErrorMessage());
			}

			return ret;
		}

		public int Unlink(uint oid)
		{
			// returns < 0 on error
			int ret = PqsqlWrapper.lo_unlink(mPGConn, oid);

			if (ret < 0)
			{
				throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Could not unlink large object {0}: {1}", oid, mConn.GetErrorMessage()));
			}

			return ret;
		}

		public int Unlink()
		{
			return Unlink(mOid);
		}

		public int Open(uint oid, LoOpen mode)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentException>(oid != 0, "Cannot open large object with InvalidOid (0)");
#else
			if (oid == 0)
				throw new ArgumentException("Cannot open large object with InvalidOid (0)");
#endif

			mFd = PqsqlWrapper.lo_open(mPGConn, oid, (int) mode);

			if (mFd < 0)
			{
				throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Cannot open large object {0}: {1}", oid, mConn.GetErrorMessage()));
			}

			mOid = oid;
			mMode = mode;
			mPos = 0;
			return mFd;
		}

		public override void Close()
		{
			if (mFd < 0)
				return;

			if (PqsqlWrapper.lo_close(mPGConn, mFd) == -1)
			{
				// ignore error
			}

			mFd = -1;
			mMode = 0;
			mPos = -1;
		}

		public override void Flush()
		{
			// noop
		}

		// sets new position in the LO
		public override long Seek(long offset, SeekOrigin origin)
		{
			if (mFd < 0)
			{
				throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Cannot seek closed large object {0}", mOid));
			}

			mPos = PqsqlWrapper.lo_lseek64(mPGConn, mFd, offset, (int) origin);

			if (mPos < 0)
			{
				throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Could not seek large object {0}: {1}", mOid, mConn.GetErrorMessage()));
			}

			return mPos;
		}

		// truncates LO to value
		public override void SetLength(long value)
		{
			if (mFd < 0)
			{
				throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Cannot truncate closed large object {0}", mOid));
			}

			int ret = PqsqlWrapper.lo_truncate64(mPGConn, mFd, value);

			if (ret < 0)
			{
				throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Could not truncate large object {0} to {1} bytes: {2}", mOid, value, mConn.GetErrorMessage()));
			}
		}

		// read from LO
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			if (!CanRead)
				throw new NotSupportedException("Reading from large object is turned off");

			int blen = buffer.Length;

			if (offset + count > blen)
				throw new ArgumentException("offset or count larger than buffer");
			
			if (offset < 0 || offset >= blen)
				throw new ArgumentOutOfRangeException(nameof(offset));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (mFd < 0)
				throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Cannot read closed large object {0}", mOid));

			int read;

			unsafe
			{
				fixed (byte* b = &buffer[offset])
				{
					read = PqsqlWrapper.lo_read(mPGConn, mFd, b, (ulong) count);
				}
			}

			if (read >= 0)
			{
				mPos += read;
				return read;
			}

			throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Could not read large object {0}: {1}", mOid, mConn.GetErrorMessage()));
		}

		// write to LO
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			if (!CanWrite)
				throw new NotSupportedException("Writing to large object is turned off");

			int blen = buffer.Length;

			if (offset + count > blen)
				throw new ArgumentException("offset or count larger than buffer");

			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (mFd < 0)
				throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Cannot write closed large object {0}", mOid));

			int ret = count;

			unsafe
			{
				while (count > 0 && ret > 0)
				{
					fixed (byte* b = &buffer[offset])
					{
						ret = PqsqlWrapper.lo_write(mPGConn, mFd, b, (ulong) count);
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
				throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Could not write to large object {0}: {1}", mOid, mConn.GetErrorMessage()));
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

		[Browsable(false)]
		public override long Length
		{
			get
			{
				long cur = mPos;
				long ret = Seek(0, SeekOrigin.End);

#if CODECONTRACTS
				Contract.Assume(ret >= 0);
#endif

				if (ret != cur)
					Seek(cur, SeekOrigin.Begin);
				return ret;
			}
		}

		[Browsable(false)]
		public override long Position
		{
			get
			{
#if CODECONTRACTS
				Contract.Assume(mPos >= 0);
#endif
				return mPos;
			}

			set
			{
				if (value < 0 || mPos == value)
					return;

				Seek(value, SeekOrigin.Begin);
			} 
		}

	}
}
