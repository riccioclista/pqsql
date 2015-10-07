using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Diagnostics.Contracts;

namespace Pqsql
{
	public class PqsqlTransaction : DbTransaction
	{
		string mTransactionString;
		IsolationLevel mIsolationLevel;
		PqsqlConnection mConn;

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
					mTransactionString = "BEGIN ISOLATION LEVEL READ COMMITTED";
					break;
				case IsolationLevel.RepeatableRead:
					mTransactionString = "BEGIN ISOLATION LEVEL REPEATABLE READ";
					break;
				case IsolationLevel.Serializable:
				case IsolationLevel.Snapshot:
					mTransactionString = "BEGIN ISOLATION LEVEL SERIALIZABLE";
					break;
				case IsolationLevel.ReadUncommitted:
					mTransactionString = "BEGIN ISOLATION LEVEL READ UNCOMMITTED";
					break;
			}

			Connection = conn;
			mIsolationLevel = isolationLevel;
			InTransaction = false;
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
				else if (mConn != value)
				{
					mConn = value;

					PqsqlCommand cmd = value.Command;
					if (cmd != null)
						cmd.Transaction = this;
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

		internal string TransactionStart
		{
			get
			{
				InTransaction = true;
				return mTransactionString;
			}
		}

		protected bool InTransaction
		{
			get;
			set;
		}

		// send commit or rollback. must not throw, used in Dispose()
		internal ExecStatus SaveTransaction(bool commit)
		{
			if (mConn == null || InTransaction == false)
				return ExecStatus.PGRES_COMMAND_OK;

			// commit or rollback
			string q = commit ? "COMMIT" : "ROLLBACK";

			// convert query string to utf8
			byte[] txnString = Encoding.UTF8.GetBytes(q);

			ExecStatus s = ExecStatus.PGRES_EMPTY_QUERY;

			unsafe
			{
				IntPtr res;
				fixed (byte* t = txnString)
				{
					res = PqsqlWrapper.PQexec(mConn.PGConnection, t);
				}

				if (res != IntPtr.Zero)
				{
					s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);
					PqsqlWrapper.PQclear(res);
				}
			}

			InTransaction = false;

			return s;
		}

		// Summary:
		//     Commits the database transaction.
		public override void Commit()
		{
			if (SaveTransaction(true) != ExecStatus.PGRES_COMMAND_OK)
			{
				string err = PqsqlWrapper.PQerrorMessage(mConn.PGConnection);
				throw new PqsqlException("Transaction commit failed: " + err);
			}
		}

		//
		// Summary:
		//     Rolls back a transaction from a pending state.
		public override void Rollback()
		{
			if (SaveTransaction(false) != ExecStatus.PGRES_COMMAND_OK)
			{
				string err = PqsqlWrapper.PQerrorMessage(mConn.PGConnection);
				throw new PqsqlException("Transaction rollback failed: " + err);
			}
		}

		#region Dispose

		//
		// Summary:
		//     Releases the unmanaged resources used by the System.Data.Common.DbTransaction.
		//public virtual void Dispose()
		//{
		//	Dispose
		//}

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
			}

			mDisposed = true;
			base.Dispose(disposing);
		}

		#endregion
	}
}
