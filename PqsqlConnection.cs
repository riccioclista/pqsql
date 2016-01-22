using System;
using System.Data.Common;
using System.Data;
using System.ComponentModel;

namespace Pqsql
{
	// When you inherit from DbConnection, you must override the following members:
	// Close, BeginDbTransaction, ChangeDatabase, CreateDbCommand, Open, and StateChange. 
	// You must also provide the following properties:
	// ConnectionString, Database, DataSource, ServerVersion, and State.
	public sealed class PqsqlConnection : DbConnection
	{
		#region libpq connection

		/// <summary>
		/// PGconn*
		/// </summary>
		private IntPtr mConnection;

		// Open / Broken / Connecting
		private ConnectionStatus mStatus;

		// Executing / Broken
		private PGTransactionStatus mTransStatus;

		private bool mNewConnectionString;

		#endregion


		#region member variables

		/// <summary>
		/// an ADO.NET connection string
		/// </summary>
		private PqsqlConnectionStringBuilder mConnectionStringBuilder;

		/// <summary>
		/// Server Version
		/// </summary>
		private int mServerVersion;

		#endregion


		#region ctors and dtors

		public PqsqlConnection(string connectionString)
			: this(new PqsqlConnectionStringBuilder(connectionString))
		{
		}

		public PqsqlConnection()
			: this(new PqsqlConnectionStringBuilder())
		{
		}

		public PqsqlConnection(PqsqlConnectionStringBuilder builder)
		{
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
			mStatus = ConnectionStatus.CONNECTION_BAD;
			mTransStatus = PGTransactionStatus.PQTRANS_UNKNOWN;
			mServerVersion = -1;
		}

		~PqsqlConnection()
		{
			Dispose(false);
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
			get { return mConnectionStringBuilder.ConnectionString; }
			set
			{
				string oldValue = mConnectionStringBuilder.ConnectionString;
				if (oldValue == null || !oldValue.Equals(value))
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
					return Convert.ToInt32((string) timeout);
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
				return mServerVersion.ToString(); 
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

				// update connection status
				mStatus = (ConnectionStatus) PqsqlWrapper.PQstatus(mConnection);

				// update transaction status
				mTransStatus = (PGTransactionStatus) PqsqlWrapper.PQtransactionStatus(mConnection);

				ConnectionState s = ConnectionState.Closed;

				switch (mStatus)
				{
					case ConnectionStatus.CONNECTION_OK:
						s |= ConnectionState.Open;
						break;

					case ConnectionStatus.CONNECTION_BAD:
						s = ConnectionState.Broken;
						break;

					default:
						s |= ConnectionState.Connecting;
						break;
				}

				switch (mTransStatus)
				{
					case PGTransactionStatus.PQTRANS_ACTIVE: /* command in progress */
						s |= ConnectionState.Executing;
						break;

					case PGTransactionStatus.PQTRANS_INERROR: /* idle, within failed transaction */
					case PGTransactionStatus.PQTRANS_UNKNOWN: /* cannot determine status */
						s = ConnectionState.Broken; // set to Broken
						break;

					/* the other two states do not contribute to the overall state:
					 * PGTransactionStatus.PQTRANS_IDLE               // connection idle
					 * PGTransactionStatus.PQTRANS_INTRANS            // idle, within transaction block
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
			if (mConnection == IntPtr.Zero)
			{
				Open();
			}

			PqsqlTransaction txn = new PqsqlTransaction(this, isolationLevel);

			// get transaction start command
			byte[] txnString = txn.TransactionStart;

			unsafe
			{
				IntPtr res;
				fixed (byte* t = txnString)
				{
					res = PqsqlWrapper.PQexec(mConnection, t);
				}

				ExecStatus s = ExecStatus.PGRES_EMPTY_QUERY;
				if (res != IntPtr.Zero)
				{
					s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);
					PqsqlWrapper.PQclear(res);
				}

				if (s != ExecStatus.PGRES_COMMAND_OK)
				{
					string err = GetErrorMessage();
					throw new PqsqlException("Transaction start failed: " + err);
				}
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
			return new PqsqlCommand(this);
		}

		//
		// Summary:
		//     Enlists in the specified transaction.
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

				// get connection status
				mStatus = (ConnectionStatus) PqsqlWrapper.PQstatus(mConnection);

				if (mStatus == ConnectionStatus.CONNECTION_BAD)
				{
					string err = GetErrorMessage();
					PqsqlWrapper.PQfinish(mConnection); // force release of mConnection memory
					Init();
					throw new PqsqlException("Could not reset connection with connection string «" + mConnectionStringBuilder.ConnectionString + "»: " + err);
				}

				// successfully reestablished connection
				return;
			}

			if (mStatus != ConnectionStatus.CONNECTION_BAD)
			{
				Close(); // force release of mConnection memory
			}

			// check connection pool for a connection
			mConnection = PqsqlConnectionPool.GetPGConn(mConnectionStringBuilder);

			if (mConnection != IntPtr.Zero)
			{
				// get connection status
				mStatus = (ConnectionStatus) PqsqlWrapper.PQstatus(mConnection);

				if (mStatus == ConnectionStatus.CONNECTION_BAD)
				{
					string err = GetErrorMessage();
					PqsqlWrapper.PQfinish(mConnection); // force release of mConnection memory
					Init();
					throw new PqsqlException("Could not create connection with connection string «" + mConnectionStringBuilder.ConnectionString + "»: " + err);
				}

				mNewConnectionString = false;
			}
			else
			{
				throw new PqsqlException("libpq: unable to allocate struct PGconn");
			}
		}


		// return current error message
		internal string GetErrorMessage()
		{
			string msg = string.Empty;

			if (mConnection != IntPtr.Zero)
			{
				unsafe
				{
					IntPtr err = new IntPtr(PqsqlWrapper.PQerrorMessage(mConnection));

					if (err != IntPtr.Zero)
					{
						msg = PqsqlUTF8Statement.CreateStringFromUTF8(err);
					}
				}
			}

			return msg;
		}

		#endregion


		#region Dispose

		public new void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

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
