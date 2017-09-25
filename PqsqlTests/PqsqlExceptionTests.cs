using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlExceptionTests
	{
		private static string connectionString = string.Empty;

		private PqsqlConnection mConnection;

		private PqsqlCommand mCmd;

		#region Additional test attributes

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			connectionString = context.Properties["connectionString"].ToString();
		}

		[TestInitialize]
		public void TestInitialize()
		{
			mConnection = new PqsqlConnection(connectionString);

			// force english error messages
			using (PqsqlCommand cmd = new PqsqlCommand("SET lc_messages TO 'en';", mConnection))
			{
				cmd.ExecuteNonQuery();
			}

			mCmd = mConnection.CreateCommand();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			mCmd.Dispose();
			mConnection.Dispose();
		}

		#endregion

		[TestMethod]
		public void PqsqlExceptionTest1()
		{
			PqsqlException e = new PqsqlException();

			string state = e.SqlState;
			int error = e.ErrorCode;
			string message = e.Message;
			string hint = e.Hint;

			Assert.AreEqual(string.Empty, message);
			Assert.AreEqual(string.Empty, hint);
			Assert.AreEqual("01000", state);
			Assert.AreEqual((int) PqsqlState.WARNING, error);
		}

		[TestMethod]
		public void PqsqlExceptionTest2()
		{
			mCmd.CommandText = "syntax error";

			try
			{
				mCmd.ExecuteNonQuery();
				Assert.Fail();
			}
			catch (PqsqlException e)
			{
				Assert.IsNotNull(e.Message);
				Assert.AreNotEqual(string.Empty, e.Message);
				Assert.AreEqual(string.Empty, e.Hint);
				Assert.AreEqual("42601", e.SqlState);
				Assert.AreEqual((int) PqsqlState.SYNTAX_ERROR, e.ErrorCode);
			}
		}

		[TestMethod]
		public void PqsqlExceptionTest3()
		{
			mCmd.CommandText = "select 1 / 0";

			try
			{
				mCmd.ExecuteNonQuery();
				Assert.Fail();
			}
			catch (PqsqlException e)
			{
				Assert.IsNotNull(e.Message);
				Assert.AreEqual("ERROR:  division by zero\n", e.Message);
				Assert.AreEqual(string.Empty, e.Hint);
				Assert.AreEqual("22012", e.SqlState);
				Assert.AreEqual((int)PqsqlState.DIVISION_BY_ZERO, e.ErrorCode);
			}
		}

		[TestMethod]
		public void PqsqlExceptionTest4()
		{
			mCmd.CommandText = "select lower(true)";

			try
			{
				mCmd.ExecuteNonQuery();
				Assert.Fail();
			}
			catch (PqsqlException e)
			{
				Assert.IsNotNull(e.Message);
				Assert.AreNotEqual(string.Empty, e.Message);
				Assert.AreEqual("No function matches the given name and argument types. You might need to add explicit type casts.", e.Hint);
				Assert.AreEqual("42883", e.SqlState);
				Assert.AreEqual((int)PqsqlState.UNDEFINED_FUNCTION, e.ErrorCode);
			}
		}
	}
}
