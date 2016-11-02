using System;
using System.Data.Common;
using System.Data;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

namespace Pqsql
{
	public sealed class PqsqlTransaction : DbTransaction
	{

		private readonly IsolationLevel mIsolationLevel;

		private PqsqlConnection mConn;

		private static readonly byte[] mBeginReadCommitted = PqsqlUTF8Statement.CreateUTF8Statement("BEGIN ISOLATION LEVEL READ COMMITTED");
		private static readonly byte[] mBeginRepeatableRead = PqsqlUTF8Statement.CreateUTF8Statement("BEGIN ISOLATION LEVEL REPEATABLE READ");
		private static readonly byte[] mBeginSerializable = PqsqlUTF8Statement.CreateUTF8Statement("BEGIN ISOLATION LEVEL SERIALIZABLE");
		private static readonly byte[] mBeginReadUncommitted = PqsqlUTF8Statement.CreateUTF8Statement("BEGIN ISOLATION LEVEL READ UNCOMMITTED");

		private static readonly byte[] mCommit = PqsqlUTF8Statement.CreateUTF8Statement("COMMIT");
		internal static readonly byte[] RollbackStatement = PqsqlUTF8Statement.CreateUTF8Statement("ROLLBACK");


		// Summary:
		//     Initializes a new System.Data.Common.DbTransaction object.
		internal PqsqlTransaction(PqsqlConnection conn)
			: this(conn, IsolationLevel.ReadCommitted)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(conn != null);
#else
			if (conn == null)
				throw new ArgumentNullException("conn");
#endif
		}

		internal PqsqlTransaction(PqsqlConnection conn, IsolationLevel isolationLevel)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(conn != null);
			Contract.Requires<ArgumentException>(isolationLevel != IsolationLevel.Chaos);
#else
			if (conn == null)
				throw new ArgumentNullException("conn");

			if (isolationLevel == IsolationLevel.Chaos)
				throw new ArgumentException("isolationLevel");
#endif

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
		internal ExecStatusType SaveTransaction(bool commit)
		{
			if (mConn == null)
				return ExecStatusType.PGRES_EMPTY_QUERY;

			// in case we are not in a transaction, report closed transaction
			if (mConn.TransactionStatus != PGTransactionStatusType.PQTRANS_INTRANS)
				return ExecStatusType.PGRES_EMPTY_QUERY;

			// if we are, either commit or rollback
			byte[] txnString = commit ? mCommit : RollbackStatement;

			return mConn.Exec(txnString);
		}

		// Summary:
		//     Commits the database transaction.
		public override void Commit()
		{
			ExecStatusType s = SaveTransaction(true);

			switch (s)
			{
				case ExecStatusType.PGRES_COMMAND_OK:
					return;

				case ExecStatusType.PGRES_EMPTY_QUERY:
					throw new PqsqlException("Cannot commit: connection or transaction is closed");

				default:
					string err = mConn.GetErrorMessage();
					throw new PqsqlException("Could not commit transaction: " + err);
			}
		}

		//
		// Summary:
		//     Rolls back a transaction from a pending state.
		public override void Rollback()
		{
			ExecStatusType s = SaveTransaction(false);

			switch (s)
			{
				case ExecStatusType.PGRES_COMMAND_OK:
					return;

				case ExecStatusType.PGRES_EMPTY_QUERY:
					throw new PqsqlException("Cannot rollback: connection or transaction is closed");

				default:
					string err = mConn.GetErrorMessage();
					throw new PqsqlException("Could not rollback transaction: " + err);
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