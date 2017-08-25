using System;
using System.Data;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlCopyFromTests
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

		[TestMethod]
		public void PqsqlCopyFromTest2()
		{
			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "CREATE TEMP TABLE temp (id int4, val int4, txt text)";
			mCmd.CommandTimeout = 200;
			mCmd.CommandType = CommandType.Text;

			const int upperbound = 2000000;
			const string text = "text value with ü and ä ";

			int q = mCmd.ExecuteNonQuery();
			Assert.AreEqual(0, q);

			PqsqlCopyFrom copy = new PqsqlCopyFrom(mConnection)
			{
				Table = "temp",
				CopyTimeout = 30
			};

			copy.Start();

			for (int i = 0; i < upperbound; i++)
			{
				int j = copy.WriteInt4(i);
				Assert.AreEqual(4, j);

				j = copy.WriteInt4(i);
				Assert.AreEqual(4, j);

				j = copy.WriteText(text + i);
				Assert.AreEqual(Encoding.UTF8.GetByteCount(text + i), j); // length without nul byte
			}
			copy.WriteNull(); // id
			copy.WriteNull(); // val
			copy.WriteNull(); // txt
			copy.End();
			copy.Close();

			mCmd.CommandText = "select * from temp";
			mCmd.CommandTimeout = 30;
			mCmd.CommandType = CommandType.Text;

			PqsqlDataReader r = mCmd.ExecuteReader();

			int n = 0;
			int k = 0;
			while (r.Read())
			{
				if (n++ == upperbound)
				{
					Assert.IsTrue(r.IsDBNull(0));
					Assert.IsTrue(r.IsDBNull(1));
					Assert.IsTrue(r.IsDBNull(2));
				}
				else
				{
					Assert.AreEqual(k, r.GetInt32(0));
					Assert.AreEqual(k, r.GetInt32(1));
					Assert.AreEqual(text + k, r.GetString(2));
					k++;
				}
			}

			Assert.AreEqual(upperbound + 1, n);

			r.Close();

			tran.Rollback();
		}

		[TestMethod]
		public void PqsqlCopyFromTest3()
		{
			PqsqlTransaction t = mConnection.BeginTransaction();

			PqsqlCommand cmd = mConnection.CreateCommand();
			cmd.Transaction = t;
			cmd.CommandText = "CREATE TEMP TABLE testcopy (c0 int2, c1 int4, c2 int8, c3 bool, c4 text, c5 float4, c6 float8, c7 timestamp, c8 interval, c9 numeric);";
			cmd.CommandTimeout = 100;
			cmd.CommandType = CommandType.Text;

			cmd.ExecuteNonQuery();

			PqsqlCopyFrom copy = new PqsqlCopyFrom(mConnection)
			{
				Table = "testcopy",
				ColumnList = "c0,c1,c2,c3,c4,c5,c6,c7,c8,c9",
				CopyTimeout = 5
			};

			copy.Start();

			DateTime now = new DateTime(2001, 1, 1, 1, 2, 3, DateTimeKind.Utc);

			for (int i = 0; i < 4; i++)
			{
				copy.WriteInt2((short) i);
				copy.WriteInt4(i);
				copy.WriteInt8(i);
				copy.WriteBool(i > 0);
				copy.WriteText(Convert.ToString(i));
				copy.WriteFloat4((float) (i + 0.123));
				copy.WriteFloat8(i + 0.123);
				copy.WriteTimestamp(now.AddSeconds(i));
				copy.WriteInterval(TimeSpan.FromHours(24) + TimeSpan.FromDays(7) + TimeSpan.FromMinutes(i));
				copy.WriteNumeric((decimal) i / 10);
			}

			copy.End();
			copy.Close();

			cmd.Transaction = t;

			cmd.CommandText = "testcopy";
			cmd.CommandType = CommandType.TableDirect;

			PqsqlDataReader r = cmd.ExecuteReader();

			Assert.AreEqual(-1, r.RecordsAffected);

			int j = 0;
			foreach (IDataRecord row in r)
			{
				Assert.AreEqual((short)j, row.GetInt16(0));
				Assert.AreEqual(j, row.GetInt32(1));
				Assert.AreEqual(j, row.GetInt64(2));
				Assert.AreEqual(j > 0, row.GetBoolean(3));
				Assert.AreEqual(Convert.ToString(j), row.GetString(4));
				Assert.AreEqual((float)(j+0.123), row.GetFloat(5));
				Assert.AreEqual(j + 0.123, row.GetDouble(6));
				Assert.AreEqual(now.AddSeconds(j), row.GetDateTime(7));
				Assert.AreEqual(TimeSpan.FromHours(24) + TimeSpan.FromDays(7) + TimeSpan.FromMinutes(j), row.GetValue(8));
				Assert.AreEqual((double)j / 10, row.GetValue(9));
				j++;
			}

			t.Rollback();
		}

		[TestMethod]
		[ExpectedException(typeof(PqsqlException), "COPY FROM timeout should have been thrown")]
		public void PqsqlCopyFromTest4()
		{
			PqsqlTransaction tran = null;
			PqsqlCopyFrom copy = null;

			try
			{
				tran = mConnection.BeginTransaction();
				mCmd.Transaction = tran;

				mCmd.CommandText = "CREATE TEMP TABLE temp (id int4, val int4, txt text)";
				mCmd.CommandTimeout = 200;
				mCmd.CommandType = CommandType.Text;

				int q = mCmd.ExecuteNonQuery();
				Assert.AreEqual(0, q);

				copy = new PqsqlCopyFrom(mConnection)
				{
					Table = "temp",
					CopyTimeout = 1
				};

				copy.Start();

				System.Threading.Thread.Sleep(1500);

				copy.End();

				Assert.Fail();
			}
			finally
			{
				copy?.Dispose();
				tran?.Dispose();
			}
		}

		[TestMethod]
		[ExpectedException(typeof(PqsqlException), "COPY FROM unexpected EOF error should have been thrown")]
		public void PqsqlCopyFromTest5()
		{
			PqsqlTransaction tran = null;
			PqsqlCopyFrom copy = null;

			try
			{
				tran = mConnection.BeginTransaction();
				mCmd.Transaction = tran;

				mCmd.CommandText = "CREATE TEMP TABLE temp (id int4, val int4, txt text)";
				mCmd.CommandTimeout = 200;
				mCmd.CommandType = CommandType.Text;

				int q = mCmd.ExecuteNonQuery();
				Assert.AreEqual(0, q);

				copy = new PqsqlCopyFrom(mConnection)
				{
					Table = "temp",
					CopyTimeout = 5
				};

				copy.Start();
				copy.WriteInt4(42); // only write one column
				copy.End();
				copy.Close();

				Assert.Fail();
			}
			finally
			{
				copy?.Dispose();
				tran?.Dispose();
			}
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException), "Table property is null should have been thrown")]
		public void PqsqlCopyFromTest6()
		{
			PqsqlTransaction tran = null;
			PqsqlCopyFrom copy = null;

			try
			{
				tran = mConnection.BeginTransaction();
				mCmd.Transaction = tran;

				copy = new PqsqlCopyFrom(mConnection)
				{
					CopyTimeout = 5
				};

				copy.Start();

				Assert.Fail();
			}
			finally
			{
				copy?.Dispose();
				tran?.Dispose();
			}
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException), "Start before write should have been thrown")]
		public void PqsqlCopyFromTest7()
		{
			PqsqlTransaction tran = null;
			PqsqlCopyFrom copy = null;

			try
			{
				tran = mConnection.BeginTransaction();
				mCmd.Transaction = tran;

				mCmd.CommandText = "CREATE TEMP TABLE temp (id int4, val int4, txt text)";
				mCmd.CommandTimeout = 200;
				mCmd.CommandType = CommandType.Text;

				int q = mCmd.ExecuteNonQuery();
				Assert.AreEqual(0, q);

				copy = new PqsqlCopyFrom(mConnection)
				{
					Table = "temp",
					CopyTimeout = 5
				};

				copy.WriteInt4(42); // write without Start()
				copy.WriteInt4(42); // write without Start()
				copy.WriteText("42"); // write without Start()
				copy.End();
				copy.Close();

				Assert.Fail();
			}
			finally
			{
				copy?.Dispose();
				tran?.Dispose();
			}
		}

	}
}
