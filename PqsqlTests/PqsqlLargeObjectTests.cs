using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlLargeObjectTests
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

		[TestMethod]
		public void PqsqlLargeObjectTest3()
		{
			PqsqlTransaction t = mConnection.BeginTransaction();

			PqsqlLargeObject low = new PqsqlLargeObject(mConnection);

			uint oid = low.Create();
			Assert.IsTrue(oid > 0);

			const int size = 20;

			byte[] wbuf = new byte[size];
			byte[] rbuf = new byte[size];

			for (int i = size - 1; i > 10; i--)
			{
				wbuf[i] = (byte) (i - 10);
			}

			const int offset = 10;
			const int len = 10;

			int fd = low.Open(oid, (LoOpen.INV_READ | LoOpen.INV_WRITE));
			Assert.IsTrue(fd >= 0);

			long wtell = low.Position;
			Assert.AreEqual(0, wtell);

			low.Write(wbuf, offset, len);

			wtell = low.Position;
			Assert.AreEqual(len, wtell);

			low.Write(wbuf, offset, len);

			wtell = low.Position;
			Assert.AreEqual(2*len, wtell);

			low.Close();

			t.Commit();

			t = mConnection.BeginTransaction();

			PqsqlLargeObject lor = new PqsqlLargeObject(mConnection);

			fd = lor.Open(oid, LoOpen.INV_READ);
			Assert.IsTrue(fd >= 0);

			long rtell = lor.Position;
			Assert.AreEqual(0, rtell);

			int read = lor.Read(rbuf, 0, len);
			Assert.AreEqual(len, read);

			rtell = lor.Position;
			Assert.AreEqual(len, rtell);

			read = lor.Read(rbuf, read, len);
			Assert.AreEqual(len, read);

			rtell = lor.Position;
			Assert.AreEqual(2*len, rtell);

			lor.Close();

			int ret = low.Unlink(oid);

			t.Commit();

			Assert.IsTrue(ret >= 0);

			for (int i = 0; i < rbuf.Length; i++)
			{
				Assert.AreEqual(wbuf[(i%10) + 10], rbuf[i]);
			}

			// oid must be gone now
			try
			{
				fd = lor.Open(oid, LoOpen.INV_READ);
				Assert.Fail();
			}
			catch (PqsqlException e)
			{
				Assert.IsNotNull(e);
			}
		}
	}
}
