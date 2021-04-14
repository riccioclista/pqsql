using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlCopyToTests
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
		public void PqsqlCopyToTest1()
		{
			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a int4, b int4, c int4); " +
							   "insert into foo values (1, 2, 3); " +
							   "insert into foo values (4, 5, 6); ";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(2, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "c,a,b",
				CopyTimeout = 10,
			};

			copy.Start();

			var i = 0;
			while (copy.FetchRow())
			{
				var c = copy.ReadInt4();
				var a = copy.ReadInt4();
				var b = copy.ReadInt4();
				
				if (i == 0)
				{
					Assert.AreEqual(1, a);
					Assert.AreEqual(2, b);
					Assert.AreEqual(3, c);
				}
				else if (i == 1)
				{
					Assert.AreEqual(4, a);
					Assert.AreEqual(5, b);
					Assert.AreEqual(6, c);
				}

				i++;
			}

			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test null values
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest2()
		{
			const int len = 1;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a int4, b int4, c int4); " +
							   "insert into foo values (null, 2, 3); ";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "c,a,b",
				CopyTimeout = 10,
			};

			copy.Start();

			var results = new int?[len];

			while (copy.FetchRow())
			{
				for (int i = 0; i < len; i++)
				{
					int? result;
					if (copy.IsNull())
					{
						result = null;
					}
					else
					{
						result = copy.ReadInt4();
					}

					if (i == 0)
					{
						// c
						Assert.IsTrue(result.HasValue);
						Assert.AreEqual(3, result.Value);
					}
					else if (i == 1)
					{
						// a
						Assert.IsFalse(result.HasValue);
					}
					else if (i == 2)
					{
						// b
						Assert.IsTrue(result.HasValue);
						Assert.AreEqual(2, result.Value);
					}
				}
			}

			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test all datatypes
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest3()
		{
			const int len = 1;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a int2, b int4, c int8, d boolean, e boolean, f float4, " +
							   "g float8, h text, i timestamp, j time, k timetz, l timetz, m date, n interval); " +
							   "insert into foo values (5, 1000001, 42949672950, true, false, 3.14, 3.14, 'hallo 1', " +
							   "TIMESTAMP '1999-01-08 04:05:06', '04:05:06.789', '04:05:06-08:00', '04:05:06+08:00', " +
							   "'1999-01-08', '3 4:05:06');";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "a,b,c,d,e,f,g,h,i,j,k,l,m,n",
				CopyTimeout = 10,
			};

			copy.Start();

			while (copy.FetchRow())
			{
				var a = copy.ReadInt2();
				Assert.AreEqual(5, a);

				var b = copy.ReadInt4();
				Assert.AreEqual(1000001, b);

				var c = copy.ReadInt8();
				Assert.AreEqual(42949672950, c);

				var d = copy.ReadBoolean();
				Assert.IsTrue(d);

				var e = copy.ReadBoolean();
				Assert.IsFalse(e);

				var f = copy.ReadFloat4();
				Assert.AreEqual(3.14, f, 0.00001);

				var g = copy.ReadFloat8();
				Assert.AreEqual(3.14, g, 0.00001);

				var h = copy.ReadString();
				Assert.AreEqual("hallo 1", h);

				var i = copy.ReadTimestamp();
				Assert.AreEqual(new DateTime(1999, 1, 8, 4, 5, 6), i);

				var j = copy.ReadTime();
				Assert.AreEqual(new DateTime(1970, 1, 1, 4, 5, 6, 789), j);

				var k = copy.ReadTimeTZ();
				Assert.AreEqual(new DateTimeOffset(1970, 1, 1, 4, 5, 6, 0, new TimeSpan(-8, 0, 0)), k);

				var l = copy.ReadTimeTZ();
				Assert.AreEqual(new DateTimeOffset(1970, 1, 1, 4, 5, 6, 0, new TimeSpan(8, 0, 0)), l);

				var m = copy.ReadDate();
				Assert.AreEqual(new DateTime(1999, 1, 8), m);

				var n = copy.ReadInterval();
				Assert.AreEqual(new TimeSpan(3, 4, 5, 6), n);
			}

			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test min/max int2/int4/int8
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest4()
		{
			const int len = 3;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a int2, b int4, c int8); " +
							   "insert into foo values (null, null, null); " +
							   "insert into foo values (-32768, -2147483648, -9223372036854775808); " +
							   "insert into foo values (32767, 2147483647, 9223372036854775807); ";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "a,b,c",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			var a0 = copy.ReadInt2();
			Assert.AreEqual(0, a0);

			var b0 = copy.ReadInt4();
			Assert.AreEqual(0, b0);

			var c0 = copy.ReadInt8();
			Assert.AreEqual(0, c0);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a1 = copy.ReadInt2();
			Assert.AreEqual(short.MinValue, a1);

			var b1 = copy.ReadInt4();
			Assert.AreEqual(int.MinValue, b1);

			var c1 = copy.ReadInt8();
			Assert.AreEqual(long.MinValue, c1);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a2 = copy.ReadInt2();
			Assert.AreEqual(short.MaxValue, a2);

			var b2 = copy.ReadInt4();
			Assert.AreEqual(int.MaxValue, b2);

			var c2 = copy.ReadInt8();
			Assert.AreEqual(long.MaxValue, c2);


			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test min/max float4/float8
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest5()
		{
			const int len = 3;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a float4, b float8); " +
							   "insert into foo values (null, null); " +
							   "insert into foo values (-3.40282347E+38, -1.7976931348623157E+308); " +
							   "insert into foo values (3.40282347E+38, 1.7976931348623157E+308); ";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "a,b",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			var a0 = copy.ReadFloat4();
			Assert.AreEqual(0, a0);

			var b0 = copy.ReadFloat8();
			Assert.AreEqual(0, b0);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a1 = copy.ReadFloat4();
			Assert.AreEqual(float.MinValue, a1);

			var b1 = copy.ReadFloat8();
			Assert.AreEqual(double.MinValue, b1);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a2 = copy.ReadFloat4();
			Assert.AreEqual(float.MaxValue, a2);

			var b2 = copy.ReadFloat8();
			Assert.AreEqual(double.MaxValue, b2);


			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test min/max timestamp, timestamptz
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest6()
		{
			const int len = 8;

			// the highest representable System.DateTime tick in postgres is 9 less than
			// System.DateTime.MaxValue.Ticks, because the resolution in postgres is to the millisecond whereas
			// System.DateTime's resolution is to 100 nanoseconds. This means the last 900 nanoseconds before the year
			// 10000 cannot be represented in postgres' timestamp.
			var postgresDateTimeMaxValueTicks = DateTime.MaxValue.Ticks - 9;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a timestamp, b timestamptz); " +
							   "insert into foo values (null, null); " +
							   "insert into foo values ('0001-01-01 00:00:00.000000 UTC', '0001-01-01 00:00:00.000000 UTC'); " +
							   "insert into foo values ('1970-01-01 00:00:00.000000 UTC', '1970-01-01 00:00:00.000000 UTC'); " +
							   "insert into foo values ('9999-12-31 23:59:59.999999 UTC', '9999-12-31 23:59:59.999999 UTC'); " +
							   "insert into foo values ('31 December, 1 BC 23:59:59.999999', '31 December, 1 BC 23:59:59.999999'); " +
							   "insert into foo values ('10000-01-01 00:00:00.000000 UTC', '10000-01-01 00:00:00.000000 UTC'); " +
							   "insert into foo values ('-infinity', '-infinity'); " +
							   "insert into foo values ('infinity', 'infinity'); ";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "a,b",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			var a0 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.MinValue, a0);

			var b0 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.MinValue, b0);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a1 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.MinValue, a1);

			var b1 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.MinValue, b1);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a2 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.UnixEpoch, a2);

			var b2 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.UnixEpoch, b2);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a3 = copy.ReadTimestamp();
			Assert.AreEqual(new DateTime(postgresDateTimeMaxValueTicks), a3);

			var b3 = copy.ReadTimestamp();
			Assert.AreEqual(new DateTime(postgresDateTimeMaxValueTicks), b3);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				// a4 '31 December, 1 BC 23:59:59.999999'
				copy.ReadTimestamp();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}

			try
			{
				// b4 '31 December, 1 BC 23:59:59.999999'
				copy.ReadTimestamp();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}


			res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				// a5 '10000-01-01 00:00:00.000000 UTC'
				copy.ReadTimestamp();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}

			try
			{
				// b5 '10000-01-01 00:00:00.000000 UTC'
				copy.ReadTimestamp();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}


			res = copy.FetchRow();
			Assert.IsTrue(res);

			// a6 '-infinity'
			var a6 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.MinValue, a6);

			// b6 '-infinity'
			var b6 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.MinValue, b6);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			// a7 'infinity'
			var a7 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.MaxValue, a7);

			// b7 'infinity'
			var b7 = copy.ReadTimestamp();
			Assert.AreEqual(DateTime.MaxValue, b7);

			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test min/max time, timetz
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest7()
		{
			const int len = 5;

			var timeMinValue = DateTime.UnixEpoch;
			var timeMaxValue = DateTime.UnixEpoch.AddDays(1);

			var timeTzMinValue = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan(14, 0, 0));
			var timeTzMaxValue = new DateTimeOffset(1970, 1, 2, 0, 0, 0, new TimeSpan(-14, 0, 0));

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a time, b timetz); " +
							   "insert into foo values (null, null); " +
							   "insert into foo values ('00:00:00', '00:00:00+14'); " +
							   "insert into foo values ('24:00:00', '24:00:00-14'); " +
							   "insert into foo values (null, '00:00:00+14:01'); " +
							   "insert into foo values (null, '24:00:00-14:01'); ";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "a,b",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			var a0 = copy.ReadTime();
			Assert.AreEqual(DateTime.MinValue, a0);

			var b0 = copy.ReadTimeTZ();
			Assert.AreEqual(DateTimeOffset.MinValue, b0);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a1 = copy.ReadTime();
			Assert.AreEqual(timeMinValue, a1);

			var b1 = copy.ReadTimeTZ();
			Assert.AreEqual(timeTzMinValue, b1);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a2 = copy.ReadTime();
			Assert.AreEqual(timeMaxValue, a2);

			var b2 = copy.ReadTimeTZ();
			Assert.AreEqual(timeTzMaxValue, b2);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			// a3
			copy.ReadTime();

			try
			{
				// b3 '00:00:00+14:01'
				copy.ReadTimeTZ();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}


			res = copy.FetchRow();
			Assert.IsTrue(res);

			// a4
			copy.ReadTime();

			try
			{
				// b4 '24:00:00-14:01'
				copy.ReadTimeTZ();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}

			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test min/max date
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest8()
		{
			const int len = 8;

			var dateMaxValue = DateTime.MaxValue.Date;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a date); " +
							   "insert into foo values (null); " +
							   "insert into foo values ('0001-01-01'); " +
							   "insert into foo values ('1970-01-01'); " +
							   "insert into foo values ('9999-12-31'); " +
							   "insert into foo values ('31 December, 1 BC'); " +
							   "insert into foo values ('10000-01-01'); " +
							   "insert into foo values ('-infinity'); " +
							   "insert into foo values ('infinity'); ";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "a",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			var a0 = copy.ReadDate();
			Assert.AreEqual(DateTime.MinValue, a0);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a1 = copy.ReadDate();
			Assert.AreEqual(DateTime.MinValue, a1);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a2 = copy.ReadDate();
			Assert.AreEqual(DateTime.UnixEpoch, a2);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a3 = copy.ReadDate();
			Assert.AreEqual(dateMaxValue, a3);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				// a4 '31 December, 1 BC'
				copy.ReadDate();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}


			res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				// a5 '10000-01-01'
				copy.ReadDate();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}


			res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				// a6 '-infinity'
				copy.ReadDate();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}


			res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				// a7 'infinity'
				copy.ReadDate();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}

			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test min/max interval
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest9()
		{
			const int len = 5;

			// the last 8 ticks (800 nanoseconds) of MinValue can not be stored in postgres' interval type, because of
			// its resolution to milliseconds.
			var intervalMinValue = new TimeSpan(TimeSpan.MinValue.Ticks + 8);

			// the last 7 ticks (700 nanoseconds) of MaxValue can not be stored in postgres' interval type, because of
			// its resolution to milliseconds.
			var intervalMaxValue = new TimeSpan(TimeSpan.MaxValue.Ticks - 7);

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a interval); " +
							   "insert into foo values (null); " +
							   "insert into foo values ('10675199 02:48:05.477580 ago'); " +
							   "insert into foo values ('10675199 02:48:05.477580'); " +
							   "insert into foo values ('10675199 02:48:05.477581 ago'); " +
							   "insert into foo values ('10675199 02:48:05.477581'); ";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "a",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			var a0 = copy.ReadInterval();
			Assert.AreEqual(TimeSpan.MinValue, a0);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a1 = copy.ReadInterval();
			Assert.AreEqual(intervalMinValue, a1);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a2 = copy.ReadInterval();
			Assert.AreEqual(intervalMaxValue, a2);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				// a3 '10675199 02:48:05.477581 ago'
				var a3 = copy.ReadInterval();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}


			res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				// a4 '10675199 02:48:05.477581'
				copy.ReadInterval();
				Assert.Fail();
			}
			catch (ArgumentOutOfRangeException) {}

			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test raw
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest10()
		{
			const int len = 2;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a bytea); " +
							   "insert into foo values (null); " +
							   "insert into foo values ('\\x707173716C'); ";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "a",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			var a0 = copy.ReadRaw();
			Assert.IsNull(a0);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a1 = copy.ReadRaw();
			CollectionAssert.AreEqual(new byte[] { 0x70, 0x71, 0x73, 0x71, 0x6C }, a1);

			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}
		
		/// <summary>
		/// Test text
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest11()
		{
			const int len = 1;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a text); " +
							   "insert into foo values ('hallo pqsql');";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				ColumnList = "a",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			var a0 = copy.ReadString();
			Assert.AreEqual("hallo pqsql", a0);

			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test query
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest12()
		{
			const int len = 3;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a int4, b text, c text); " +
							   "insert into foo values (2, 'hallo pqsql 2', null); " +
							   "insert into foo values (1, 'hallo pqsql 1', 'asd'); " +
							   "insert into foo values (3, 'hallo pqsql 3', null);";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Query = "select a,b from foo order by a asc; ",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			var a0 = copy.ReadInt4();
			Assert.AreEqual(1, a0);

			var b0 = copy.ReadString();
			Assert.AreEqual("hallo pqsql 1", b0);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a1 = copy.ReadInt4();
			Assert.AreEqual(2, a1);

			var b1 = copy.ReadString();
			Assert.AreEqual("hallo pqsql 2", b1);


			res = copy.FetchRow();
			Assert.IsTrue(res);

			var a2 = copy.ReadInt4();
			Assert.AreEqual(3, a2);

			var b2 = copy.ReadString();
			Assert.AreEqual("hallo pqsql 3", b2);

			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test with table name only (and wrong reader used)
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest13()
		{
			const int len = 1;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a int4, b text, c text); " +
							   "insert into foo values (2, 'hallo pqsql 2', null);";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				CopyTimeout = 10,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				copy.ReadBoolean();
			}
			catch (InvalidOperationException) {}

			// try again with correct reader
			var a0 = copy.ReadInt4();
			Assert.AreEqual(2, a0);

			var b0 = copy.ReadString();
			Assert.AreEqual("hallo pqsql 2", b0);

			var c0 = copy.IsNull();
			Assert.IsTrue(c0);

			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}

		/// <summary>
		/// Test with table name only and suppress schema query (and wrong reader used)
		/// </summary>
		[TestMethod]
		public void PqsqlCopyToTest14()
		{
			const int len = 1;

			PqsqlTransaction tran = mConnection.BeginTransaction();
			mCmd.Transaction = tran;

			mCmd.CommandText = "create temporary table foo (a int4, b text, c text); " +
							   "insert into foo values (2, 'hallo pqsql 2', null);";
			mCmd.CommandType = CommandType.Text;
			int affected = mCmd.ExecuteNonQuery();
			Assert.AreEqual(len, affected);

			var copy = new PqsqlCopyTo(mConnection)
			{
				Table = "foo",
				CopyTimeout = 10,
				SuppressSchemaQuery = true,
			};

			copy.Start();

			var res = copy.FetchRow();
			Assert.IsTrue(res);

			try
			{
				copy.ReadBoolean();
			}
			catch (InvalidOperationException) {}

			// try again with correct reader
			var a0 = copy.ReadInt4();
			Assert.AreEqual(2, a0);

			var b0 = copy.ReadString();
			Assert.AreEqual("hallo pqsql 2", b0);

			var c0 = copy.IsNull();
			Assert.IsTrue(c0);

			res = copy.FetchRow();
			Assert.IsFalse(res);
			copy.Close();
			tran.Rollback();
		}
	}
}
