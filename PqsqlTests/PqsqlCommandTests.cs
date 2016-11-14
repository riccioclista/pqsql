using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlCommandTests
	{
		private PqsqlConnection mConnection;

		private PqsqlCommand mCmd;

		#region Additional test attributes

		[TestInitialize]
		public void MyTestInitialize()
		{
			mConnection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");
			mCmd = mConnection.CreateCommand();
		}

		[TestCleanup]
		public void MyTestCleanup()
		{
			mCmd.Dispose();
			mConnection.Dispose();
		}

		#endregion

		[TestMethod]
		public void PqsqlCommandTest1()
		{
			mCmd.CommandText = "select pg_sleep(3);";
			PqsqlDataReader r = mCmd.ExecuteReader();

			bool b = r.Read();
			Assert.AreEqual(true, b);

			object v = r.GetValue(0);
			Assert.AreEqual("", v);
		}
	}
}
