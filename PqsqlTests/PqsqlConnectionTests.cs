using System;
using System.Data;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	/// <summary>
	/// Summary description for PqsqlConnectionTests
	/// </summary>
	[TestClass]
	public class PqsqlConnectionTests
	{

		[TestMethod]
		public void PqsqlConnectionTest1()
		{
			PqsqlConnection connection = new PqsqlConnection("");

			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");
			Assert.AreEqual(String.Empty, connection.ConnectionString, "wrong connection string");
			Assert.AreEqual(0, connection.ConnectionTimeout, "wrong connection timeout");
			Assert.AreEqual(String.Empty, connection.Database, "wrong connection database");
		}

		[TestMethod]
		public void PqsqlConnectionTest2()
		{
			PqsqlConnection connection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");

			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");
			Assert.AreEqual("host=localhost;port=5432;user=postgres;dbname=postgres;connect_timeout=3", connection.ConnectionString, "wrong connection string");
			Assert.AreEqual(3, connection.ConnectionTimeout, "wrong connection timeout");
			Assert.AreEqual("postgres", connection.Database, "wrong connection database");

			connection.Open();

			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			connection.Close();

			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");
		}

		[TestMethod]
		public void PqsqlConnectionTest3()
		{
			PqsqlConnection connection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			IntPtr conn_attempt1 = connection.PGConnection;

			connection.Close();
			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");

			// in order to simulate broken connections in the connection pool, we can step through this unit test and turn
			// off postgresql before we open the connection again in the next step below:
			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			IntPtr conn_attempt2 = connection.PGConnection;

			Assert.AreEqual(conn_attempt1, conn_attempt2, "connection was not received from internal connection pool");
		}

		[DllImport("ws2_32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int closesocket(IntPtr s);

		[TestMethod]
		public void PqsqlConnectionTest4()
		{
			PqsqlConnection connection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			IntPtr conn_attempt1 = connection.PGConnection;

			int sock1 = PqsqlWrapper.PQsocket(conn_attempt1);
			Assert.AreNotEqual(-1, sock1, "wrong socket");
			IntPtr sockhandle1 = (IntPtr) sock1;

			// close the underlying socket without letting Pqsql and libpq know
			int closed = closesocket(sockhandle1);
			Assert.AreEqual(0, closed, "closesocket failed");

			connection.Close();
			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");

			connection.Open();
			IntPtr conn_attempt2 = connection.PGConnection;

			ConnStatusType status = PqsqlWrapper.PQstatus(conn_attempt2);
			Assert.AreEqual(ConnStatusType.CONNECTION_OK, status, "connection broken");
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			int sock2 = PqsqlWrapper.PQsocket(conn_attempt2);
			IntPtr sockhandle2 = (IntPtr) sock2;

			// close the underlying socket without letting Pqsql and libpq know
			closed = closesocket(sockhandle2);
			Assert.AreEqual(0, closed, "closesocket failed");
		}

		[TestMethod]
		public void PqsqlConnectionTest5()
		{
			PqsqlConnection connection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			PqsqlTransaction trans = connection.BeginTransaction();
			Assert.AreEqual(IsolationLevel.ReadCommitted, trans.IsolationLevel, "wrong transaction isolation level");

			trans.Rollback();
			connection.Close();
			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");
		}

		[TestMethod]
		[ExpectedException(typeof(PqsqlException), "The connection should have been terminated.")]
		public void PqsqlConnectionTest6()
		{
			PqsqlConnection connection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			PqsqlCommand cmd = connection.CreateCommand();

			Assert.AreEqual(CommandType.Text, cmd.CommandType, "wrong command type");
			Assert.AreEqual(-1, cmd.CommandTimeout, "wrong command timeout");
			Assert.AreEqual(string.Empty, cmd.CommandText, "wrong command text");
			Assert.AreEqual(connection, cmd.Connection, "wrong command connection");
			Assert.AreEqual(null, cmd.Transaction, "wrong command transaction");

			cmd.CommandText = "select pg_terminate_backend(pg_backend_pid()); select pg_sleep(5);";

			cmd.ExecuteNonQuery();

			cmd.ExecuteNonQuery();

			cmd.Cancel();

			cmd.CommandText = "";

			cmd.ExecuteNonQuery();
		}

		[TestMethod]
		public void PqsqlConnectionTest7()
		{
			PqsqlConnection connection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");

			bool opened = false;
			bool closed = true;

			connection.StateChange += (sender, args) =>
			{
				if (args.CurrentState == ConnectionState.Closed)
				{
					opened = false;
					closed = true;
				}

				if (args.CurrentState == ConnectionState.Open)
				{
					opened = true;
					closed = false;
				}
			};

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			Assert.AreEqual(true, opened);
			Assert.AreEqual(false, closed);

			try
			{
				PqsqlCommand cmd = connection.CreateCommand();
				cmd.CommandText = "select pg_terminate_backend(pg_backend_pid()); select pg_sleep(5);";
				cmd.ExecuteNonQuery();
				cmd.ExecuteNonQuery();
			}
			catch (Exception)
			{
				// ignored
				connection.Close();
			}

			Assert.AreEqual(false, opened);
			Assert.AreEqual(true, closed);
		}
	}
}
