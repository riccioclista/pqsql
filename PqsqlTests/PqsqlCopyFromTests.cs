using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlCopyFromTests
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
		public void PqsqlCopyFromTest1()
		{
			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo ( a int2, b int4, c int8 )";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(0, affected);

			PqsqlCopyFrom copy = new PqsqlCopyFrom(mConnection)
			{
				Table = "foo",
				ColumnList = "c,a,b",
				CopyTimeout = 10
			};

			copy.Start();

			for (short i = 9; i >= 0; i--)
			{
				copy.WriteInt8(i);
				copy.WriteInt2(i);
				copy.WriteInt4(i);
			}
			
			copy.End();

			copy.Close();

			mCmd.CommandText = "foo";
			mCmd.CommandType = CommandType.TableDirect;

			int value = 9;
			foreach (IDataRecord rec in mCmd.ExecuteReader())
			{
				object[] o = new object[3];
				rec.GetValues(o);

				Assert.IsInstanceOfType(o[0], typeof(short));
				Assert.AreEqual((short) value, o[0]);
				Assert.IsInstanceOfType(o[1], typeof(int));
				Assert.AreEqual(value, o[1]);
				Assert.IsInstanceOfType(o[2], typeof(long));
				Assert.AreEqual((long) value, o[2]);

				value--;
			}
	
			Assert.AreEqual(-1, value);

			tran.Rollback();
		}
	}
}
