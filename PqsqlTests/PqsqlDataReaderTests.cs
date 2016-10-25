using System.Collections;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlDataReaderTests
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
		public void PqsqlDataReaderTest1()
		{
			// TODO we don't support xid and inet datatypes yet
			mCmd.CommandText = "select datid,datname,pid,application_name,backend_start,waiting,query from pg_stat_activity";
			PqsqlDataReader reader = mCmd.ExecuteReader();
			Assert.AreEqual(false, reader.IsClosed);
			Assert.AreEqual(7, reader.FieldCount);

			DataTable dt = reader.GetSchemaTable();
			Assert.AreNotEqual(null, dt);

			reader.Close();
			Assert.AreEqual(ConnectionState.Open, mConnection.State);
			Assert.AreEqual(true, reader.IsClosed);
		}

		[TestMethod]
		public void PqsqlDataReaderTest2()
		{
			// TODO we don't support xid and inet datatypes yet
			mCmd.CommandText = "select datid,datname,pid,application_name,backend_start,waiting,query from pg_stat_activity";
			PqsqlDataReader reader = mCmd.ExecuteReader(CommandBehavior.CloseConnection);
			Assert.AreEqual(false, reader.IsClosed);
			Assert.AreEqual(-1, reader.RecordsAffected);

			int read = 0;

			foreach (object o in reader)
			{
				read++;
			}

			// we must have at least one connection open (our one)
			Assert.AreNotEqual(0, read);

			reader.Close();

			Assert.AreEqual(ConnectionState.Closed, mConnection.State);
			Assert.AreEqual(true, reader.IsClosed);
		}
	}
}
