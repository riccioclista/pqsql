using System;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Diagnostics.Contracts;

namespace Pqsql
{
	public class PqsqlTransaction : DbTransaction
	{
		IsolationLevel mIsolationLevel;
		PqsqlConnection mConn;

		private static readonly byte[] mBeginReadCommitted = Encoding.UTF8.GetBytes("BEGIN ISOLATION LEVEL READ COMMITTED");
		private static readonly byte[] mBeginRepeatableRead = Encoding.UTF8.GetBytes("BEGIN ISOLATION LEVEL REPEATABLE READ");
		private static readonly byte[] mBeginSerializable = Encoding.UTF8.GetBytes("BEGIN ISOLATION LEVEL SERIALIZABLE");
		private static readonly byte[] mBeginReadUncommitted = Encoding.UTF8.GetBytes("BEGIN ISOLATION LEVEL READ UNCOMMITTED");

		private static readonly byte[] mCommit = Encoding.UTF8.GetBytes("COMMIT");
		private static readonly byte[] mRollback = Encoding.UTF8.GetBytes("ROLLBACK");

		// Summary:
		//     Initializes a new System.Data.Common.DbTransaction object.
		internal PqsqlTransaction(PqsqlConnection conn)
			: this(conn, IsolationLevel.ReadCommitted)
		{
			Contract.Requires(conn != null);
		}

		internal PqsqlTransaction(PqsqlConnection conn, IsolationLevel isolationLevel)
		{
			Contract.Requires(conn != null);
			Contract.Requires(isolationLevel != IsolationLevel.Chaos);

			switch (isolationLevel)
			{
				case IsolationLevel.ReadCommitted:
				case IsolationLevel.Unspecified:
					isolationLevel = IsolationLevel.ReadCommitted;
					TransactionStart = mBeginReadCommitted;
					break;
				case IsolationLevel.RepeatableRead:
					TransactionStart = mBeginRepeatableRead;
					break;
				case IsolationLevel.Serializable:
				case IsolationLevel.Snapshot:
					TransactionStart = mBeginSerializable;
					break;
				case IsolationLevel.ReadUncommitted:
					TransactionStart = mBeginReadUncommitted;
					break;
			}

			Connection = conn;
			mIsolationLevel = isolationLevel;
		}

		~PqsqlTransaction()
		{
			Dispose(false);
		}


		// Summary:
		//     Specifies the System.Data.Common.DbConnection object associated with the
		//     transaction.
		//
		// Returns:
		//     The System.Data.Common.DbConnection object associated with the transaction.
		public new PqsqlConnection Connection
		{
			get
			{
				return mConn;
			}
			internal set
			{
				if (value == null)
				{
					mConn = null;
				}
				// ReSharper disable once RedundantCheckBeforeAssignment
				else if (mConn != value)
				{
					mConn = value;
				}
			}
		}

		//
		// Summary:
		//     Specifies the System.Data.Common.DbConnection object associated with the
		//     transaction.
		//
		// Returns:
		//     The System.Data.Common.DbConnection object associated with the transaction.
		protected override DbConnection DbConnection
		{
			get
			{
				return Connection;
			}
		}

		//
		// Summary:
		//     Specifies the System.Data.IsolationLevel for this transaction.
		//
		// Returns:
		//     The System.Data.IsolationLevel for this transaction.
		public override IsolationLevel IsolationLevel
		{
			get
			{
				return mIsolationLevel;
			}
		}

		internal byte[] TransactionStart
		{
			get;
			set;
		}

		// send commit or rollback. must not throw, used in Dispose()
		internal ExecStatus SaveTransaction(bool commit)
		{
			ExecStatus s = ExecStatus.PGRES_EMPTY_QUERY;

			if (mConn != null)
			{
				IntPtr pgconn = mConn.PGConnection;

				PGTransactionStatus ts = (PGTransactionStatus) PqsqlWrapper.PQtransactionStatus(pgconn);

				if (ts != PGTransactionStatus.PQTRANS_INTRANS)
					return ExecStatus.PGRES_EMPTY_QUERY;

				// commit or rollback
				byte[] txnString = commit ? mCommit : mRollback;

				unsafe
				{
					IntPtr res;
					fixed (byte* t = txnString)
					{
						res = PqsqlWrapper.PQexec(pgconn, t);
					}

					if (res != IntPtr.Zero)
					{
						s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);
						PqsqlWrapper.PQclear(res);
					}
				}
			}

			return s;
		}

		// Summary:
		//     Commits the database transaction.
		public override void Commit()
		{
			ExecStatus s = SaveTransaction(true);

			switch (s)
			{
				case ExecStatus.PGRES_COMMAND_OK:
					return;

				case ExecStatus.PGRES_EMPTY_QUERY:
					throw new PqsqlException("Cannot commit: no transaction open");

				default:
					string err = mConn.GetErrorMessage();
					throw new PqsqlException(err);
			}
		}

		//
		// Summary:
		//     Rolls back a transaction from a pending state.
		public override void Rollback()
		{
			ExecStatus s = SaveTransaction(true);

			switch (s)
			{
				case ExecStatus.PGRES_COMMAND_OK:
					return;

				case ExecStatus.PGRES_EMPTY_QUERY:
					throw new PqsqlException("Cannot rollback: no transaction open");

				default:
					string err = mConn.GetErrorMessage();
					throw new PqsqlException(err);
			}
		}

		#region Dispose

		//
		// Summary:
		//     Releases the unmanaged resources used by the System.Data.Common.DbTransaction.
		public new void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool mDisposed;

		//
		// Summary:
		//     Releases the unmanaged resources used by the System.Data.Common.DbTransaction
		//     and optionally releases the managed resources.
		//
		// Parameters:
		//   disposing:
		//     If true, this method releases all resources held by any managed objects that
		//     this System.Data.Common.DbTransaction references.
		protected override void Dispose(bool disposing)
		{
			if (mDisposed)
			{
				return;
			}

			if (disposing)
			{
				SaveTransaction(false); // send rollback if we are in a transaction
				mConn = null;
			}

			base.Dispose(disposing);
			mDisposed = true;
		}

		#endregion
	}
}