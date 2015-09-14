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

		/// <summary>
		/// 
		/// </summary>
		protected Pqsql.ConnectionStatus mStatus;

		#endregion


		#region member variables

		/// <summary>
		/// an ADO.NET connection string
		/// </summary>
		protected PqsqlConnectionStringBuilder mConnectionStringBuilder;

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
			mConnection = IntPtr.Zero;
			mStatus = ConnectionStatus.CONNECTION_BAD;
			mConnectionStringBuilder = builder;
		}

		~PqsqlConnection()
		{
			Dispose(false);
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
		[RecommendedAsConfigurable(true)]
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
				if (mConnectionStringBuilder.ContainsKey(PqsqlConnectionStringBuilder.connect_timeout))
				{
					return (int)mConnectionStringBuilder[PqsqlConnectionStringBuilder.connect_timeout];
				}
				else
				{
					return 0;
				}
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
				if (mConnectionStringBuilder.ContainsKey(PqsqlConnectionStringBuilder.dbname))
				{
					return (string)mConnectionStringBuilder[PqsqlConnectionStringBuilder.dbname];
				}
				else
				{
					return string.Empty;
				}
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
				if (mConnectionStringBuilder.ContainsKey(PqsqlConnectionStringBuilder.host))
				{
					return (string)mConnectionStringBuilder[PqsqlConnectionStringBuilder.host];
				}
				else
				{
					return string.Empty;
				}

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
				return string.Empty; // TODO
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
		public virtual ConnectionState State
		{
			get
			{
				if (mConnection == IntPtr.Zero)
					return ConnectionState.Closed;

				// TODO get updated connection status

				switch (mStatus)
				{
					case ConnectionStatus.CONNECTION_OK:
						return ConnectionState.Open;

					case ConnectionStatus.CONNECTION_BAD:
						return ConnectionState.Broken;

					case ConnectionStatus.CONNECTION_MADE: // TODO
						return ConnectionState.Executing;

					case ConnectionStatus.CONNECTION_AWAITING_RESPONSE: // TODO
						return ConnectionState.Fetching;

					default:
						return ConnectionState.Connecting;
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
			return new PqsqlTransaction(this, isolationLevel);
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
			// close connection and release memory in any case
			if (mConnection != IntPtr.Zero)
			{
				PqsqlWrapper.PQfinish(mConnection);
				mConnection = IntPtr.Zero;
				mStatus = ConnectionStatus.CONNECTION_BAD;
				return;
			}

			// TODO exception
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
		//     Opens a database connection with the settings specified by the System.Data.Common.DbConnection.ConnectionString.
		public void Open()
		{
			if (mStatus != ConnectionStatus.CONNECTION_BAD)
			{
				Close();
			}

			// setup null-terminated key-value arrays for the connection
			string[] keys = new string[mConnectionStringBuilder.Keys.Count + 1];
			string[] vals = new string[mConnectionStringBuilder.Values.Count + 1];

			// copy over
			mConnectionStringBuilder.Keys.CopyTo(keys, 0);
			mConnectionStringBuilder.Values.CopyTo(vals, 0);

			// now create connection
			mConnection = PqsqlWrapper.PQconnectdbParams(keys, vals, 0);

			if (mConnection != IntPtr.Zero)
			{
				mStatus = (Pqsql.ConnectionStatus)PqsqlWrapper.PQstatus(mConnection);
				if (mStatus == ConnectionStatus.CONNECTION_BAD)
				{
					Close(); // force release of mConnection memory

					// TODO exception
				}
			}
			else
			{
				
			}
		}

		#endregion


		#region Dispose



		public void Dispose()
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
