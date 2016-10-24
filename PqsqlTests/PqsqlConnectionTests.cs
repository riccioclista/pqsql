using System;
using System.Data;
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

		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

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
			PqsqlConnection connection = new PqsqlConnection("host=localhost; port=5432; dbname=postgres; connect_timeout=3");

			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");
			Assert.AreEqual("host=localhost;port=5432;dbname=postgres;connect_timeout=3", connection.ConnectionString, "wrong connection string");
			Assert.AreEqual(3, connection.ConnectionTimeout, "wrong connection timeout");
			Assert.AreEqual("postgres", connection.Database, "wrong connection database");

			connection.Open();

			Assert.AreEqual(ConnectionState.Open, connection.State, "wrong connection state");

			connection.Close();

			Assert.AreEqual(ConnectionState.Closed, connection.State, "wrong connection state");
		}
	}
}
