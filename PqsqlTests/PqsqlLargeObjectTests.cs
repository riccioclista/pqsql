using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlLargeObjectTests
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
		public void PqsqlLargeObjectTest1()
		{
			PqsqlTransaction tran = mConnection.BeginTransaction();

			PqsqlLargeObject lo = new PqsqlLargeObject(mConnection);

			uint loid = lo.Create();
			Assert.IsTrue(loid > 0);

			lo.Open(loid, LoOpen.INV_READ | LoOpen.INV_WRITE);
			Assert.AreEqual(0, lo.Position);

			byte[] b = Encoding.ASCII.GetBytes("abc");
			lo.Write(b, 0, b.Length);

			Assert.AreEqual(3, lo.Position);
			Assert.AreEqual(3, lo.Length);
			lo.Close();

			Assert.IsTrue(lo.Unlink() >= 0);

			tran.Rollback();
		}

		[TestMethod]
		[ExpectedException(typeof(PqsqlException), "The large object should not have been instantiated.")]
		public void PqsqlLargeObjectTest2()
		{
			PqsqlLargeObject lo = new PqsqlLargeObject(mConnection);
			Assert.Fail();
		}
	}
}
