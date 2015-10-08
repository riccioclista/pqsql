using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.ComponentModel;

namespace Pqsql
{
	// When you inherit from DbConnection, you must override the following members:
	// Close, BeginDbTransaction, ChangeDatabase, CreateDbCommand, Open, and StateChange. 
	// You must also provide the following properties:
	// ConnectionString, Database, DataSource, ServerVersion, and State.
	public class PqsqlConnection : DbConnection
	{
		#region libpq connection

		/// <summary>
		/// PGconn*
		/// </summary>
		protected IntPtr mConnection;

		// good / bad / connecting connection (fetching / executing connection comes from mCmd.State bits)
		private Pqsql.ConnectionStatus mStatus;

		#endregion


		#region member variables

		// used to check PqsqlCommand.State bits
		protected PqsqlCommand mCmd;

		/// <summary>
		/// an ADO.NET connection string
		/// </summary>
		protected PqsqlConnectionStringBuilder mConnectionStringBuilder;

		/// <summary>
		/// Server Version
		/// </summary>
		protected int mServerVersion;

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
			: base()
		{
			Init();
			mConnectionStringBuilder = builder;
		}

		/// <summary>
		/// initializes all member variables, except mConnectionStringBuilder
		/// who will only be set in the ctors once and for all
		/// </summary>
		protected void Init()
		{
			mConnection = IntPtr.Zero;
			mStatus = ConnectionStatus.CONNECTION_BAD;
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
			set { mConnectionStringBuilder.ConnectionString = value; }
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
				object timeout = "0";
				mConnectionStringBuilder.TryGetValue(PqsqlConnectionStringBuilder.connect_timeout, out timeout);
				return Convert.ToInt32((string)timeout);
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
				object dbname = string.Empty;
				mConnectionStringBuilder.TryGetValue(PqsqlConnectionStringBuilder.dbname, out dbname);
				return (string)dbname;
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
				object ds = string.Empty;
				mConnectionStringBuilder.TryGetValue(PqsqlConnectionStringBuilder.host, out ds);
				return (string)ds;
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
				mStatus = (Pqsql.ConnectionStatus) PqsqlWrapper.PQstatus(mConnection);
				
				switch (mStatus)
				{
					// get ConnectionState.Executing / ConnectionState.Fetching bits from PqsqlCommand.State
					case ConnectionStatus.CONNECTION_OK:
						return ( ConnectionState.Open | (mCmd == null ? 0 : mCmd.State) );

					case ConnectionStatus.CONNECTION_BAD:
						return ConnectionState.Broken;

					default:
						return ConnectionState.Connecting;
				}
			}
		}

		[Browsable(false)]
		internal PqsqlCommand Command
		{
			get
			{
				return mCmd;
			}
			set
			{
				if (value == null)
				{
					mCmd = null;
				}
				else if (mCmd != value)
				{
					mCmd = value;
					value.Connection = this;
				}
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
			PqsqlTransaction txn = new PqsqlTransaction(this, isolationLevel);

			// convert query string to utf8
			byte[] txnString = Encoding.UTF8.GetBytes(txn.TransactionStart);

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
			throw new NotImplementedException("ChangeDatabase is not implemented");
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

			PqsqlWrapper.PQfinish(mConnection); // close connection and release memory
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
			if (mStatus != ConnectionStatus.CONNECTION_BAD)
			{
				Close(); // force release of mConnection memory
			}

			// setup null-terminated key-value arrays for the connection
			string[] keys = new string[mConnectionStringBuilder.Keys.Count + 1];
			string[] vals = new string[mConnectionStringBuilder.Values.Count + 1];

			// get keys and values from PqsqlConnectionStringBuilder
			mConnectionStringBuilder.Keys.CopyTo(keys, 0);
			mConnectionStringBuilder.Values.CopyTo(vals, 0);

			// now create connection
			mConnection = PqsqlWrapper.PQconnectdbParams(keys, vals, 0);

			if (mConnection != IntPtr.Zero)
			{
				// get connection status
				mStatus = (Pqsql.ConnectionStatus) PqsqlWrapper.PQstatus(mConnection);

				if (mStatus == ConnectionStatus.CONNECTION_BAD)
				{
					string err = GetErrorMessage();
					Close(); // force release of mConnection memory
					throw new PqsqlException(err);
				}
			}
			else
			{
				throw new PqsqlException("libpq: unable to allocate struct PGconn");
			}
		}


		// return current error message (TODO: currently no UTF8 conversion performed)
		internal string GetErrorMessage()
		{
			string msg = string.Empty;

			if (mConnection != IntPtr.Zero)
			{
				unsafe
				{
					sbyte* err = PqsqlWrapper.PQerrorMessage(mConnection);

					if (err != null)
					{
						msg = new string(err);
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

		bool mDisposed = false;

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
