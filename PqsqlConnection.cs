using System;
using System.Data.Common;
using System.Data;
using System.ComponentModel;
using System.Globalization;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

using PqsqlWrapper = Pqsql.UnsafeNativeMethods.PqsqlWrapper;

namespace Pqsql
{
	// When you inherit from DbConnection, you must override the following members:
	// Close, BeginDbTransaction, ChangeDatabase, CreateDbCommand, Open, and StateChange. 
	// You must also provide the following properties:
	// ConnectionString, Database, DataSource, ServerVersion, and State.
	public sealed class PqsqlConnection : DbConnection
	{
		private static readonly byte[] mTimeZoneParameter = PqsqlUTF8Statement.CreateUTF8Statement("TimeZone");

		#region libpq connection

		/// <summary>
		/// PGconn*
		/// </summary>
		private IntPtr mConnection;

		// Open / Broken / Connecting
		private ConnStatusType mStatus;

		// Executing / Broken
		private PGTransactionStatusType mTransStatus;

		private bool mNewConnectionString;

		#endregion


		#region member variables

		/// <summary>
		/// an ADO.NET connection string
		/// </summary>
		private readonly PqsqlConnectionStringBuilder mConnectionStringBuilder;

		/// <summary>
		/// Server Version
		/// </summary>
		private int mServerVersion;

		#endregion

#if CODECONTRACTS
		[ContractInvariantMethod]
		private void ClassInvariant()
		{
			Contract.Invariant(mConnectionStringBuilder != null);
		}
#endif

		#region ctors and dtors

		public PqsqlConnection(string connectionString)
			: this(new PqsqlConnectionStringBuilder(connectionString))
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(connectionString != null);
#else
			if (connectionString == null)
				throw new ArgumentNullException(nameof(connectionString));
#endif
		}

		public PqsqlConnection()
			: this(new PqsqlConnectionStringBuilder())
		{
		}

		public PqsqlConnection(PqsqlConnectionStringBuilder builder)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(builder != null);
#else
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));
#endif

			Init();
			mConnectionStringBuilder = builder;
		}

		/// <summary>
		/// initializes all member variables, except mConnectionStringBuilder
		/// who will only be set in the ctors once and for all
		/// </summary>
		private void Init()
		{
			mNewConnectionString = true;
			mConnection = IntPtr.Zero;
			mStatus = ConnStatusType.CONNECTION_BAD;
			mTransStatus = PGTransactionStatusType.PQTRANS_UNKNOWN;
			mServerVersion = -1;
		}

		#endregion


		#region PGConn*

		internal IntPtr PGConnection
		{
			get
			{
				return mConnection;
			}
		}

		#endregion


		#region DbConnection

		// Summary:
		//     Gets or sets the string used to open the connection.
		//
		// Returns:
		//     The connection string used to establish the initial connection. The exact
		//     contents of the connection string depend on the specific data source for
		//     this connection. The default value is an empty string.
		[RefreshProperties(RefreshProperties.All)]
		[DefaultValue("")]
		[SettingsBindable(true)]
		public override string ConnectionString
		{
			get
			{
#if CODECONTRACTS
				Contract.Ensures(Contract.Result<System.String>() != null);
#endif

				return mConnectionStringBuilder.ConnectionString;
			}
			set
			{
				if (!string.IsNullOrEmpty(value) && !mConnectionStringBuilder.ConnectionString.Equals(value))
				{
					mConnectionStringBuilder.ConnectionString = value;
					mNewConnectionString = true;
				}
			}
		}

		//
		// Summary:
		//     Gets the time to wait while establishing a connection before terminating
		//     the attempt and generating an error.
		//
		// Returns:
		//     The time (in seconds) to wait for a connection to open. The default value
		//     is determined by the specific type of connection that you are using.
		public override int ConnectionTimeout
		{
			get
			{
				object timeout;

				if (mConnectionStringBuilder.TryGetValue(PqsqlConnectionStringBuilder.connect_timeout, out timeout))
				{
					return Convert.ToInt32((string) timeout, CultureInfo.InvariantCulture);
				}

				return 0;
			}
		}

		//
		// Summary:
		//     Gets the name of the current database after a connection is opened, or the
		//     database name specified in the connection string before the connection is
		//     opened.
		//
		// Returns:
		//     The name of the current database or the name of the database to be used after
		//     a connection is opened. The default value is an empty string.
		public override string Database
		{
			get
			{
				object dbname;

				if (mConnectionStringBuilder.TryGetValue(PqsqlConnectionStringBuilder.dbname, out dbname))
				{
					return (string) dbname;
				}

				return string.Empty;
			}
		}

		//
		// Summary:
		//     Gets the name of the database server to which to connect.
		//
		// Returns:
		//     The name of the database server to which to connect. The default value is
		//     an empty string.
		public override string DataSource
		{
			get
			{
				object ds;

				if (mConnectionStringBuilder.TryGetValue(PqsqlConnectionStringBuilder.host, out ds))
				{
					return (string) ds;
				}

				return string.Empty;
			}
		}

		//
		// Summary:
		//     Gets the application name of the connection
		//
		// Returns:
		//     The application name stored in PqsqlConnectionStringBuilder. The default value is
		//     an empty string.
		public string ApplicationName
		{
			get
			{
				object appname;

				if (mConnectionStringBuilder.TryGetValue(PqsqlConnectionStringBuilder.application_name, out appname))
				{
					return (string) appname;
				}

				return string.Empty;
			}
		}

		//
		// Summary:
		//     Gets a string that represents the version of the server to which the object
		//     is connected.
		//
		// Returns:
		//     The version of the database. The format of the string returned depends on
		//     the specific type of connection you are using.
		[Browsable(false)]
		public override string ServerVersion
		{
			get
			{
				if (mServerVersion == -1 && mConnection != IntPtr.Zero)
				{
					mServerVersion = PqsqlWrapper.PQserverVersion(mConnection);
				}
				return mServerVersion.ToString(CultureInfo.InvariantCulture);
			}
		}

		//
		// Summary:
		//     Gets the current TimeZone parameter setting of the server.
		//
		// Returns:
		//     Certain parameter values are reported by the server automatically at connection startup or whenever their values change.
		//     set timezone="TIMEZONENAME" will update this parameter setting. The default value is an empty string.
		public string TimeZone
		{
			get
			{
#if CODECONTRACTS
				Contract.Ensures(Contract.Result<System.String>() != null);
#endif

				if (mConnection == IntPtr.Zero)
					return string.Empty;

				string tz;

				unsafe
				{
					fixed (byte* timezone = mTimeZoneParameter)
					{
						sbyte* tzb = PqsqlWrapper.PQparameterStatus(mConnection, timezone);

						if (tzb == null)
							tz = string.Empty;
						else
							tz = new string(tzb); // TODO UTF-8 encoding ignored here!
					}
				}

				return tz;
			}
		}

		//
		// Summary:
		//     Gets the transaction state of the connection.
		//
		// Returns:
		//     The transaction state of the connection.
		[Browsable(false)]
		internal PGTransactionStatusType TransactionStatus
		{
			get
			{
				mTransStatus = (mConnection == IntPtr.Zero) ?
					PGTransactionStatusType.PQTRANS_UNKNOWN : // unknown transaction status
					PqsqlWrapper.PQtransactionStatus(mConnection); // update transaction status
				return mTransStatus;
			}
		}

		//
		// Summary:
		//     Gets the state of the connection.
		//
		// Returns:
		//     The state of the connection.
		[Browsable(false)]
		internal ConnStatusType Status
		{
			get
			{
				mStatus = (mConnection == IntPtr.Zero) ?
					ConnStatusType.CONNECTION_BAD : // broken connection
					PqsqlWrapper.PQstatus(mConnection);
				return mStatus;
			}
		}

		//
		// Summary:
		//     Gets a string that describes the state of the connection.
		//
		// Returns:
		//     The state of the connection. The format of the string returned depends on
		//     the specific type of connection you are using.
		[Browsable(false)]
		public override ConnectionState State
		{
			get
			{
				if (mConnection == IntPtr.Zero)
					return ConnectionState.Closed;

				ConnectionState s = ConnectionState.Closed; // 0

				// updates connection status
				switch (Status)
				{
					case ConnStatusType.CONNECTION_OK:
						s |= ConnectionState.Open;
						break;

					case ConnStatusType.CONNECTION_BAD:
						s = ConnectionState.Broken;
						break;

					default:
						s |= ConnectionState.Connecting;
						break;
				}

				// updates transaction status
				switch (TransactionStatus)
				{
					case PGTransactionStatusType.PQTRANS_ACTIVE: /* command in progress */
						s |= ConnectionState.Executing;
						break;

					case PGTransactionStatusType.PQTRANS_INERROR: /* idle, within failed transaction */
					case PGTransactionStatusType.PQTRANS_UNKNOWN: /* cannot determine status */
						s = ConnectionState.Broken; // set to Broken
						break;

					/* the other two states do not contribute to the overall state:
					 * PGTransactionStatusType.PQTRANS_IDLE               // connection idle
					 * PGTransactionStatusType.PQTRANS_INTRANS            // idle, within transaction block
					 */
				}

				return s;
			}
		}
		
		// Summary:
		//     Starts a database transaction.
		//
		// Parameters:
		//   isolationLevel:
		//     Specifies the isolation level for the transaction.
		//
		// Returns:
		//     An object representing the new transaction.
		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		{
#if CODECONTRACTS
			Contract.Assume(isolationLevel != IsolationLevel.Chaos);
#endif
			return BeginTransaction(isolationLevel);
		}

		//
		// Summary:
		//     Starts a database transaction.
		//
		// Returns:
		//     An object representing the new transaction.
		public new PqsqlTransaction BeginTransaction()
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<PqsqlTransaction>() != null);
#endif

			return BeginTransaction(IsolationLevel.Unspecified);
		}

		//
		// Summary:
		//     Starts a database transaction with the specified isolation level.
		//
		// Parameters:
		//   isolationLevel:
		//     Specifies the isolation level for the transaction.
		//
		// Returns:
		//     An object representing the new transaction.
		public new PqsqlTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentException>(isolationLevel != IsolationLevel.Chaos);
			Contract.Ensures(Contract.Result<PqsqlTransaction>() != null);
#else
			if (isolationLevel == IsolationLevel.Chaos)
				throw new ArgumentException("isolationLevel == IsolationLevel.Chaos");
#endif

			if (mConnection == IntPtr.Zero)
			{
				Open();
			}

			PqsqlTransaction txn = new PqsqlTransaction(this, isolationLevel);

			// get transaction start command
			byte[] txnString = txn.TransactionStart;

			ExecStatusType s = Exec(txnString);

			if (s != ExecStatusType.PGRES_COMMAND_OK)
			{
				string err = GetErrorMessage();
				throw new PqsqlException("Transaction start failed: " + err);
			}

			return txn;
		}

		//
		// Summary:
		//     Changes the current database for an open connection.
		//
		// Parameters:
		//   databaseName:
		//     Specifies the name of the database for the connection to use.
		public override void ChangeDatabase(string databaseName)
		{
			if (mConnectionStringBuilder.ContainsKey(PqsqlConnectionStringBuilder.dbname))
			{
				mConnectionStringBuilder.Remove(PqsqlConnectionStringBuilder.dbname);
			}
			mConnectionStringBuilder.Add(PqsqlConnectionStringBuilder.dbname, databaseName);
			Close();
			Open();
		}

		//
		// Summary:
		//     Closes the connection to the database. This is the preferred method of closing
		//     any open connection.
		//
		// Exceptions:
		//   System.Data.Common.DbException:
		//     The connection-level error that occurred while opening the connection.
		public override void Close()
		{
			if (mConnection == IntPtr.Zero)
				return;

			// release connection to the pool for mConnectionStringBuilder
			PqsqlConnectionPool.ReleasePGConn(mConnectionStringBuilder, mConnection);

			Init(); // reset state, next Open() call might end up at a different server / db

			OnStateChange(new StateChangeEventArgs(ConnectionState.Open, ConnectionState.Closed));
		}

		//
		// Summary:
		//     Creates and returns a System.Data.Common.DbCommand object associated with
		//     the current connection.
		//
		// Returns:
		//     A System.Data.Common.DbCommand object.
		protected override DbCommand CreateDbCommand()
		{
			return CreateCommand();
		}

		//
		// Summary:
		//     Creates and returns a System.Data.Common.DbCommand object associated with
		//     the current connection.
		//
		// Returns:
		//     A System.Data.Common.DbCommand object.
		public new PqsqlCommand CreateCommand()
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<PqsqlCommand>() != null);
#endif

			return new PqsqlCommand(this);
		}

		//
		// Summary:
		//     Enlists in the specified transaction. TODO
		//
		// Parameters:
		//   transaction:
		//     A reference to an existing System.Transactions.Transaction in which to enlist.
		//public override void EnlistTransaction(PqsqlTransaction transaction)
		//{
		//	throw new NotImplementedException();
		//}

		//
		// Summary:
		//     Opens a database connection with the settings specified by the System.Data.Common.DbConnection.ConnectionString.
		public override void Open()
		{
			if (mConnection != IntPtr.Zero && !mNewConnectionString)
			{
				// close and open with current connection setting
				PqsqlWrapper.PQreset(mConnection);

				// update connection and transaction status
				if (Status == ConnStatusType.CONNECTION_BAD || TransactionStatus != PGTransactionStatusType.PQTRANS_IDLE)
				{
					string err = GetErrorMessage();
					PqsqlWrapper.PQfinish(mConnection); // force release of mConnection memory
					Init();
					throw new PqsqlException("Could not reset connection with connection string «" + mConnectionStringBuilder.ConnectionString + "»: " + err, (int) PqsqlState.CONNECTION_FAILURE);
				}

				OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));

				// successfully reestablished connection
				return;
			}

			if (mStatus != ConnStatusType.CONNECTION_BAD)
			{
				Close(); // force release of mConnection memory
			}

			// check connection pool for a connection
			mConnection = PqsqlConnectionPool.GetPGConn(mConnectionStringBuilder, out mStatus, out mTransStatus);

			if (mConnection == IntPtr.Zero)
				throw new PqsqlException("libpq: unable to allocate struct PGconn");

			// check connection and transaction status
			if (mStatus == ConnStatusType.CONNECTION_BAD || mTransStatus != PGTransactionStatusType.PQTRANS_IDLE)
			{
				string err = GetErrorMessage();
				PqsqlWrapper.PQfinish(mConnection); // force release of mConnection memory
				Init();
				throw new PqsqlException("Could not create connection with connection string «" + mConnectionStringBuilder.ConnectionString + "»: " + err);
			}

			mNewConnectionString = false;

			OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
		}

		// call PQexec and immediately discard PGresult struct
		internal ExecStatusType Exec(byte[] stmt)
		{
			IntPtr res;
			ExecStatusType s = Exec(stmt, out res);
			Consume(res);
			return s;
		}

		// call PQexec
		internal ExecStatusType Exec(byte[] stmt, out IntPtr res)
		{
			if (mConnection == IntPtr.Zero)
			{
				res = IntPtr.Zero;
				return ExecStatusType.PGRES_FATAL_ERROR;
			}

			unsafe
			{
				fixed (byte* st = stmt)
				{
					res = PqsqlWrapper.PQexec(mConnection, st);
				}
			}

			ExecStatusType s = ExecStatusType.PGRES_FATAL_ERROR;

			if (res != IntPtr.Zero)
			{
				s = PqsqlWrapper.PQresultStatus(res);
			}

			return s;
		}

		// consume remaining results from connection
		internal void Consume(IntPtr res)
		{
			if (mConnection == IntPtr.Zero)
				return;

			if (res != IntPtr.Zero)
			{
				PqsqlWrapper.PQclear(res);
			}

			// consume all remaining results until we reach the NULL result
			while ((res = PqsqlWrapper.PQgetResult(mConnection)) != IntPtr.Zero)
			{
				// always free mResult
				PqsqlWrapper.PQclear(res);
			}
		}

		// return current error message
		internal string GetErrorMessage()
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<string>() != null);
#endif

			string msg = string.Empty;

			if (mConnection != IntPtr.Zero)
			{
				unsafe
				{
					byte *b = (byte*) PqsqlWrapper.PQerrorMessage(mConnection);

					if (b != null)
					{
						msg = PqsqlUTF8Statement.CreateStringFromUTF8(b);
					}
				}
			}

			return msg;
		}

		#endregion


		#region Dispose

		bool mDisposed;

		protected override void Dispose(bool disposing)
		{
			if (mDisposed)
			{
				return;
			}

			// always release mConnection (must not throw exception)
			Close();
			
			base.Dispose(disposing);
			mDisposed = true;
		}

		#endregion

	}
}
