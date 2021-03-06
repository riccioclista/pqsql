﻿using System;
using System.Data;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

using PqsqlWrapper = Pqsql.UnsafeNativeMethods.PqsqlWrapper;

namespace PqsqlTests
{
	/// <summary>
	/// Summary description for PqsqlConnectionTests
	/// </summary>
	[TestClass]
	public class PqsqlConnectionTests
	{
		private static string connectionString = string.Empty;

		#region Additional test attributes

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			connectionString = context.Properties["connectionString"].ToString();
		}

		[TestInitialize]
		public void TestInitialize()
		{
		}

		[TestCleanup]
		public void TestCleanup()
		{
		}

		#endregion
		[TestMethod]
		public void PqsqlConnectionTest1()
		{
			PqsqlConnection connection = new PqsqlConnection(string.Empty);

			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");
			Assert.AreEqual(string.Empty, connection.ConnectionString, "wrong connection string");
			Assert.AreEqual(0, connection.ConnectionTimeout, "wrong connection timeout");
			Assert.AreEqual(string.Empty, connection.Database, "wrong connection database");

			connection.ConnectionString = connectionString;

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			connection.ChangeDatabase("postgres");
			Assert.AreEqual("postgres", connection.Database, "wrong connection database");

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			ExecStatusType status = connection.Exec(null);
			Assert.AreEqual(ExecStatusType.PGRES_FATAL_ERROR, status);

			IntPtr res;
			status = connection.Exec(null, out res);
			Assert.AreEqual(ExecStatusType.PGRES_FATAL_ERROR, status);
			Assert.AreEqual(IntPtr.Zero, res);
			connection.Consume(res);

			connection.Dispose();
			connection.Dispose();
		}

		[TestMethod]
		public void PqsqlConnectionTest2()
		{
			PqsqlConnection connection = new PqsqlConnection(connectionString);

			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");
			Assert.AreEqual(connectionString.Replace(" ","").Replace("\t","").Replace("\n",""), connection.ConnectionString, "wrong connection string");
			Assert.AreEqual(3, connection.ConnectionTimeout, "wrong connection timeout");

			// closed connection with service file should give us empty database name
			string dbname = connection.Database;
			Assert.AreEqual(string.Empty, dbname, "wrong connection database");

			string serverVersion = connection.ServerVersion;
			Assert.IsFalse(string.IsNullOrEmpty(serverVersion));
			Assert.AreEqual("-1", serverVersion);

			connection.Open();

			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			dbname = connection.Database;
			Assert.AreNotEqual(string.Empty, dbname, "wrong connection database");

			serverVersion = connection.ServerVersion;
			Assert.IsFalse(string.IsNullOrEmpty(serverVersion));

			if (serverVersion.Length >= 6) // expect 100000 or later
			{
				int version = Convert.ToInt32(serverVersion);
				Assert.IsTrue(version >= 100000);
			}
			else if (serverVersion.Length >= 5) // expect 90400 .. 90699
			{
				int version = Convert.ToInt32(serverVersion);
				Assert.IsTrue(version >= 90400);
				Assert.IsTrue(version <= 90699);
			}
			else
			{
				Assert.Fail("wrong server version");
			}

			connection.Close();

			dbname = connection.Database;
			Assert.AreEqual(string.Empty, dbname, "wrong connection database");

			serverVersion = connection.ServerVersion;
			Assert.IsFalse(string.IsNullOrEmpty(serverVersion));
			Assert.AreEqual("-1", serverVersion);

			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");
		}

		[TestMethod]
		public void PqsqlConnectionTest3()
		{
			PqsqlConnectionPool.Clear();

			PqsqlConnection connection = new PqsqlConnection(connectionString);

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

#if WIN32
		[DllImport("ws2_32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int closesocket(IntPtr s);
#else
		[DllImport("libc", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern int close(int fd);
#endif

		private int CloseSocket(int fd)
		{
#if WIN32
			var handle = (IntPtr) fd;
			return closesocket(handle);
#else
			return close(fd);
#endif
		}

		[TestMethod]
		public void PqsqlConnectionTest4()
		{
			PqsqlConnection connection = new PqsqlConnection(connectionString);

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			IntPtr conn_attempt1 = connection.PGConnection;

			int sock1 = PqsqlWrapper.PQsocket(conn_attempt1);
			Assert.AreNotEqual(-1, sock1, "wrong socket");

			// close the underlying socket without letting Pqsql and libpq know
			int closed = CloseSocket(sock1);
			Assert.AreEqual(0, closed, "closesocket failed");

			connection.Close();
			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");

			connection.Open();
			IntPtr conn_attempt2 = connection.PGConnection;

			ConnStatusType status = PqsqlWrapper.PQstatus(conn_attempt2);
			Assert.AreEqual(ConnStatusType.CONNECTION_OK, status, "connection broken");
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			int sock2 = PqsqlWrapper.PQsocket(conn_attempt2);

			// close the underlying socket without letting Pqsql and libpq know
			closed = CloseSocket(sock2);
			Assert.AreEqual(0, closed, "closesocket failed");
		}

		[TestMethod]
		public void PqsqlConnectionTest5()
		{
			PqsqlConnection connection = new PqsqlConnection(connectionString);

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
			PqsqlConnection connection = new PqsqlConnection(connectionString);

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			PqsqlCommand cmd = connection.CreateCommand();

			Assert.AreEqual(CommandType.Text, cmd.CommandType, "wrong command type");
			Assert.AreEqual(-1, cmd.CommandTimeout, "wrong command timeout");
			Assert.AreEqual(string.Empty, cmd.CommandText, "wrong command text");
			Assert.AreEqual(connection, cmd.Connection, "wrong command connection");
			Assert.AreEqual(null, cmd.Transaction, "wrong command transaction");

			cmd.CommandText = "select pg_terminate_backend(pg_backend_pid()); select pg_sleep(5);";

			cmd.ExecuteNonQuery(); // must execute both statements

			cmd.Cancel();

			cmd.CommandText = "";

			cmd.ExecuteNonQuery();
		}

		[TestMethod]
		public void PqsqlConnectionTest7()
		{
			PqsqlConnection connection = new PqsqlConnection(connectionString);

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
				cmd.ExecuteNonQuery(); // must execute both statements
			}
			catch (Exception)
			{
				// ignored
				connection.Close();
			}

			Assert.AreEqual(false, opened);
			Assert.AreEqual(true, closed);
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException), "PqsqlConnection.GetSchema")]
		public void PqsqlConnectionTest8()
		{
			PqsqlConnection connection = new PqsqlConnection(connectionString);
			DataTable connschema = connection.GetSchema();
			Assert.Fail();
		}

		[TestMethod]
		public void PqsqlConnectionTest9()
		{
			PqsqlConnection connection = new PqsqlConnection(connectionString);
			string s = connection.GetErrorMessage();
			Assert.AreEqual(string.Empty, s);

			PqsqlCommand cmd = connection.CreateCommand();
			cmd.CommandText = "foobar command";

			try
			{
				cmd.ExecuteNonQuery();
			}
			catch (PqsqlException exception)
			{
				// SYNTAX_ERROR = 16801924, // 42601 (syntax_error)
				//Assert.AreNotSame(string.Empty, exception.Hint);
				Assert.AreEqual("42601", exception.SqlState);
				Assert.AreEqual(16801924, exception.ErrorCode);
			}
			
			s = connection.GetErrorMessage();
			Assert.IsNotNull(s);
			Assert.AreNotSame(string.Empty, s);
		}

		[TestMethod]
		public void PqsqlConnectionTest10()
		{
			PqsqlConnectionPool.Clear();

			PqsqlConnection connection = new PqsqlConnection(connectionString);

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			IntPtr pgConn = connection.PGConnection;
			int client_encoding = PqsqlWrapper.PQclientEncoding(pgConn);
			Assert.AreEqual((int) PgEnc.PG_UTF8, client_encoding, "wrong client_encoding");

			// setup client_encoding to sqlascii, and leave it like that

			byte[] encoding = PqsqlUTF8Statement.CreateUTF8Statement("sqlascii");
			client_encoding = PqsqlWrapper.PQsetClientEncoding(pgConn, encoding);
			Assert.AreEqual(0, client_encoding, "could not set client_encoding");

			client_encoding = PqsqlWrapper.PQclientEncoding(pgConn);
			Assert.AreEqual(0, client_encoding, "wrong client_encoding");

			connection.Close();

			// now try to get the same connection again

			connection = new PqsqlConnection(connectionString);

			connection.Open();
			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			pgConn = connection.PGConnection;
			client_encoding = PqsqlWrapper.PQclientEncoding(pgConn);
			Assert.AreEqual((int)PgEnc.PG_UTF8, client_encoding, "wrong client_encoding");
		}

	}
}
