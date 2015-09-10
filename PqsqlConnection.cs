using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.ComponentModel;

namespace Pqsql
{
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
		protected string mConnectionString;

		#endregion


		#region ctors and dtors

		public PqsqlConnection(string connectionString) : this()
		{
			mConnectionString = connectionString;
		}

		public PqsqlConnection() : base()
		{
			mConnection = IntPtr.Zero;
			mStatus = ConnectionStatus.CONNECTION_BAD;
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
		public virtual string ConnectionString { get; set; }

		//
		// Summary:
		//     Gets the time to wait while establishing a connection before terminating
		//     the attempt and generating an error.
		//
		// Returns:
		//     The time (in seconds) to wait for a connection to open. The default value
		//     is determined by the specific type of connection that you are using.
		public override int ConnectionTimeout { get; }

		//
		// Summary:
		//     Gets the name of the current database after a connection is opened, or the
		//     database name specified in the connection string before the connection is
		//     opened.
		//
		// Returns:
		//     The name of the current database or the name of the database to be used after
		//     a connection is opened. The default value is an empty string.
		public virtual string Database { get; }

		//
		// Summary:
		//     Gets the name of the database server to which to connect.
		//
		// Returns:
		//     The name of the database server to which to connect. The default value is
		//     an empty string.
		public virtual string DataSource { get; }

		//
		// Summary:
		//     Gets a string that represents the version of the server to which the object
		//     is connected.
		//
		// Returns:
		//     The version of the database. The format of the string returned depends on
		//     the specific type of connection you are using.
		[Browsable(false)]
		public virtual string ServerVersion { get; }

		//
		// Summary:
		//     Gets a string that describes the state of the connection.
		//
		// Returns:
		//     The state of the connection. The format of the string returned depends on
		//     the specific type of connection you are using.
		[Browsable(false)]
		public virtual ConnectionState State { get; }

		// Summary:
		//     Starts a database transaction.
		//
		// Parameters:
		//   isolationLevel:
		//     Specifies the isolation level for the transaction.
		//
		// Returns:
		//     An object representing the new transaction.
		protected virtual DbTransaction BeginDbTransaction(IsolationLevel isolationLevel);

		//
		// Summary:
		//     Changes the current database for an open connection.
		//
		// Parameters:
		//   databaseName:
		//     Specifies the name of the database for the connection to use.
		public virtual void ChangeDatabase(string databaseName);

		//
		// Summary:
		//     Closes the connection to the database. This is the preferred method of closing
		//     any open connection.
		//
		// Exceptions:
		//   System.Data.Common.DbException:
		//     The connection-level error that occurred while opening the connection.
		public void Close()
		{
			// close connection and release memory in any case
			if (mConnection != IntPtr.Zero)
			{
				PqsqlWrapper.PQfinish(mConnection);
				mConnection = IntPtr.Zero;
				mStatus = ConnectionStatus.CONNECTION_BAD;
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
		protected virtual DbCommand CreateDbCommand()
		{
			return null;
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

			mConnection = PqsqlWrapper.PQconnectdb(mConnectionString);

			if (mConnection != IntPtr.Zero)
			{
				mStatus = (Pqsql.ConnectionStatus)PqsqlWrapper.PQstatus(mConnection);
				if (mStatus == ConnectionStatus.CONNECTION_BAD)
				{
					Close(); // force release memory of mConnection

					// TODO exception
				}
			}
			else
			{
				mStatus = Pqsql.ConnectionStatus.CONNECTION_BAD;
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
