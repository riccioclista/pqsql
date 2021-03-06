﻿using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlDataReaderTests
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

			// force UTC
			using (var cmd = new PqsqlCommand("set timezone to 'UTC';", mConnection))
			{
				cmd.ExecuteNonQuery();
			}

			mCmd = mConnection.CreateCommand();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			mCmd?.Dispose();
			mConnection?.Dispose();
		}

		#endregion


		[TestMethod]
		public void PqsqlDataReaderTest1()
		{
			// TODO we don't support xid and inet datatypes yet
			mCmd.CommandText = "select datid,datname,pid,application_name,backend_start,query from pg_stat_activity";
			PqsqlDataReader reader = mCmd.ExecuteReader();
			Assert.AreEqual(false, reader.IsClosed);
			Assert.AreEqual(6, reader.FieldCount);

			DataTable dt = reader.GetSchemaTable();
			Assert.AreNotEqual(null, dt);

			Assert.AreEqual(6, dt.Rows.Count);

			Assert.AreEqual("datid", dt.Rows[0][SchemaTableColumn.ColumnName]);
			Assert.AreEqual(1, dt.Rows[0][SchemaTableColumn.ColumnOrdinal]);

			Assert.AreEqual("datname", dt.Rows[1][SchemaTableColumn.ColumnName]);
			Assert.AreEqual(2, dt.Rows[1][SchemaTableColumn.ColumnOrdinal]);

			Assert.AreEqual("pid", dt.Rows[2][SchemaTableColumn.ColumnName]);
			Assert.AreEqual(3, dt.Rows[2][SchemaTableColumn.ColumnOrdinal]);

			Assert.AreEqual("application_name", dt.Rows[3][SchemaTableColumn.ColumnName]);
			Assert.AreEqual(4, dt.Rows[3][SchemaTableColumn.ColumnOrdinal]);

			Assert.AreEqual("backend_start", dt.Rows[4][SchemaTableColumn.ColumnName]);
			Assert.AreEqual(5, dt.Rows[4][SchemaTableColumn.ColumnOrdinal]);

			Assert.AreEqual("query", dt.Rows[5][SchemaTableColumn.ColumnName]);
			Assert.AreEqual(6, dt.Rows[5][SchemaTableColumn.ColumnOrdinal]);

			reader.Close();
			Assert.AreEqual(ConnectionState.Open, mConnection.State);
			Assert.AreEqual(true, reader.IsClosed);
		}

		[TestMethod]
		public void PqsqlDataReaderTest2()
		{
			// TODO we don't support xid and inet datatypes yet
			mCmd.CommandText = "select datid,datname,pid,application_name,backend_start,query from pg_stat_activity";
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

		[TestMethod]
		public void PqsqlDataReaderTest3()
		{
			mCmd.CommandText = "select datid,datname,pid,application_name,backend_start,query from pg_stat_activity";
			PqsqlDataReader reader = mCmd.ExecuteReader(CommandBehavior.CloseConnection);
			Assert.AreEqual(false, reader.IsClosed);
			Assert.AreEqual(-1, reader.RecordsAffected);

			int ordinal = reader.GetOrdinal("application_name");

			// application_name is the 4th column
			Assert.AreEqual(3, ordinal);

			reader.Close();

			Assert.AreEqual(ConnectionState.Closed, mConnection.State);
			Assert.AreEqual(true, reader.IsClosed);
		}

		[TestMethod]
		public void PqsqlDataReaderTest4()
		{
			mCmd.CommandText = "select :arr";

			PqsqlParameter arr = new PqsqlParameter
			{
				ParameterName = ":arr",
				PqsqlDbType = PqsqlDbType.Array | PqsqlDbType.Boolean,
				Value = new bool[] { true, true, false, false }
			};

			mCmd.Parameters.Add(arr);

			using (PqsqlDataReader reader = mCmd.ExecuteReader(CommandBehavior.CloseConnection))
			{
				bool read = reader.Read();
				Assert.IsTrue(read);
				object o = reader.GetValue(0);

				// postgres returns 1-based array bool[1..4] in o
				// whereas arr.Value is 0-based array bool[]
				CollectionAssert.AreEqual((ICollection) arr.Value, (ICollection) o);// round trip succeeded
			}
		}

		[TestMethod]
		public void PqsqlDataReaderTest5()
		{
			mConnection.Open();

			// get dbname from connection
			object dbname = mConnection.Database;

			PqsqlDataReader reader = new PqsqlDataReader(mCmd, CommandBehavior.SchemaOnly, new[] {"select oid as ObjectId from pg_class"});
			DataTable schematable = reader.GetSchemaTable();

			Assert.IsNotNull(schematable);

			Assert.AreEqual(13, schematable.Columns.Count);
		
			DataRowCollection rows = schematable.Rows;
			Assert.AreEqual(1, rows.Count);
			
			DataRow row = rows[0];

			Assert.AreEqual(PqsqlDbType.Oid, (PqsqlDbType) row[PqsqlSchemaTableColumn.TypeOid]);
			Assert.AreEqual(true, row[SchemaTableColumn.AllowDBNull]);
			Assert.AreEqual("objectid", row[SchemaTableColumn.BaseColumnName]);
			Assert.AreEqual(dbname, row[SchemaTableOptionalColumn.BaseCatalogName]);
			Assert.AreEqual("pg_catalog", row[SchemaTableColumn.BaseSchemaName]);
			Assert.AreEqual("pg_class", row[SchemaTableColumn.BaseTableName]);
			Assert.AreEqual("objectid", row[SchemaTableColumn.ColumnName]);

			Assert.AreEqual(1, row[SchemaTableColumn.ColumnOrdinal]);
			Assert.AreEqual(4, row[SchemaTableColumn.ColumnSize]);

			Assert.AreEqual(0, row[SchemaTableColumn.NumericPrecision]);
			Assert.AreEqual(0, row[SchemaTableColumn.NumericScale]);

			Assert.AreEqual("oid", row[SchemaTableColumn.ProviderType]);
			Assert.AreEqual(typeof(uint), row[SchemaTableColumn.DataType]);
		}

		[TestMethod]
		public void PqsqlDataReaderTest6()
		{
			const string qs = @"select ARRAY[0,1,2,3,42,null,4711]::_text, :p7, unnest(:p6), unnest(:p8) ;
								select interval '20 days', interval '123 secs', interval '20 years 10 months', now(), :p4, timestamp 'infinity', timestamp '-infinity', date 'infinity', date '-infinity';
								select 123.456::numeric, :p3::text ;
								select 'NaN'::numeric, '-1234567898765432123456789.87654321'::numeric ;
								select generate_series(1,1000000),generate_series(1,1000000);
								select generate_series(1,1000000),generate_series(1,1000000);
								select :p1,:p2::text ;
								select extract(epoch from date_trunc('day',current_date - :p9 ))::integer ";

			const int p1_val = -1;
			const int p2_val = 2;
			const int p3_val = -3;
			const double p4_val = 3.1415925;

			decimal p5_val = decimal.Parse("123456789,87654321");

			int[] p6_arr = { 1, 42, 4711, 0815 };

			const string p7_0 = "1 string mit ü and ä sowie 0";
			const string p7_1 = "2 string mit ü and ä sowie 🐨";
			const string p7_2 = "42 string mit ü and ä sowie е́";
			const string p7_3 = "0815 string mit ü and ä sowie П";

			decimal[] p8_arr = {decimal.MinValue, 1.23M, 12.345M, -123.4567M, decimal.MaxValue};

			const int p9_val = 47;

			mCmd.CommandText = qs;
			mCmd.CommandTimeout = 20;

			PqsqlParameter p1 = mCmd.CreateParameter();
			p1.ParameterName = "p1";
			p1.Value = p1_val;
			p1.DbType = DbType.Int32;

			PqsqlParameter p2 = mCmd.CreateParameter();
			p2.ParameterName = "p2";
			p2.Value = p2_val;
			p2.DbType = DbType.Int32;

			PqsqlParameter p3 = mCmd.CreateParameter();
			p3.ParameterName = "p3";
			p3.Value = p3_val;
			p3.DbType = DbType.Int32;

			PqsqlParameter p4 = mCmd.CreateParameter();
			p4.ParameterName = "p4";
			p4.Value = p4_val;
			p4.DbType = DbType.Double;

			PqsqlParameter p5 = mCmd.CreateParameter();
			p5.ParameterName = "p5";
			p5.Value = p5_val;
			p5.DbType = DbType.Decimal;

			PqsqlParameter p6 = mCmd.CreateParameter();
			p6.ParameterName = "p6";
			p6.Value = p6_arr;
			p6.PqsqlDbType = PqsqlDbType.Array | PqsqlDbType.Int4;

			PqsqlParameter p7 = mCmd.CreateParameter();
			p7.ParameterName = "p7";
			Array b = Array.CreateInstance(typeof(string), new int[] { 4 }, new int[] { -1 });
			b.SetValue(p7_0, -1);
			b.SetValue(p7_1, 0);
			b.SetValue(p7_2, 1);
			b.SetValue(p7_3, 2);
			p7.Value = b;
			p7.PqsqlDbType = PqsqlDbType.Array | PqsqlDbType.Text;

			PqsqlParameter p8 = mCmd.CreateParameter();
			p8.ParameterName = "p8";
			Array c = Array.CreateInstance(typeof(decimal), new int[] { 5 }, new int[] { -2 });
			c.SetValue(p8_arr[0], -2);
			c.SetValue(p8_arr[1], -1);
			c.SetValue(p8_arr[2], 0);
			c.SetValue(p8_arr[3], 1);
			c.SetValue(p8_arr[4], 2);
			p8.Value = c;
			p8.PqsqlDbType = PqsqlDbType.Array | PqsqlDbType.Numeric;

			PqsqlParameter p9 = mCmd.CreateParameter();
			p9.ParameterName = "p9";
			p9.Value = p9_val;
			// let Pqsql guess the parameter type

			mCmd.Parameters.Add(p1);
			mCmd.Parameters.Add(p2);
			mCmd.Parameters.Add(p3);
			mCmd.Parameters.Add(p4);
			mCmd.Parameters.Add(p5);
			mCmd.Parameters.Add(p6);
			mCmd.Parameters.Add(p7);
			mCmd.Parameters.Add(p8);
			mCmd.Parameters.Add(p9);

			PqsqlTransaction t = mConnection.BeginTransaction();
			mCmd.Transaction = t;

			PqsqlDataReader r = mCmd.ExecuteReader();

			int p6_rt = 0;
			//int p8_rt = 0;

			// select ARRAY[0,1,2,3,42,null,4711]::_text, :p7, unnest(:p6), unnest(:p8) 
			while (r.Read())
			{
				object o0 = r.GetValue(0);
				object o1 = r.GetValue(1);
				object o2 = r.GetValue(2);
				object o3 = r.GetValue(3);

				Array arr = (Array) o0;

				Assert.AreEqual("0", arr.GetValue(1));
				Assert.AreEqual("1", arr.GetValue(2));
				Assert.AreEqual("2", arr.GetValue(3));
				Assert.AreEqual("3", arr.GetValue(4));
				Assert.AreEqual("42", arr.GetValue(5));
				Assert.AreEqual(null, arr.GetValue(6));
				Assert.AreEqual("4711", arr.GetValue(7));

				arr = (Array) o1;

				Assert.AreEqual(p7_0, arr.GetValue(1));
				Assert.AreEqual(p7_1, arr.GetValue(2));
				Assert.AreEqual(p7_2, arr.GetValue(3));
				Assert.AreEqual(p7_3, arr.GetValue(4));

				Assert.AreEqual(p6_arr[p6_rt++ % 4], o2);

				// TODO decimal vs. double: decimal.MinValue cast to double does not work here
				//Assert.AreEqual(p8_arr[p8_rt++], o3);
			}

			bool next = r.NextResult();
			Assert.IsTrue(next);

			// select interval '20 days', interval '123 secs', interval '20 years 10 months', now(), :p4, timestamp 'infinity', timestamp '-infinity', date 'infinity', date '-infinity'
			while (r.Read())
			{
				TimeSpan s = r.GetTimeSpan(0);
				Assert.AreEqual(TimeSpan.FromDays(20), s);

				s = r.GetTimeSpan(1);
				Assert.AreEqual(TimeSpan.FromSeconds(123), s);

				s = r.GetTimeSpan(2);
				Assert.AreEqual(TimeSpan.FromDays(7609), s);

				DateTime d = r.GetDateTime(3);
				TimeSpan diff = DateTime.UtcNow - d;
				Assert.IsTrue(diff < TimeSpan.FromHours(1));
				Assert.IsTrue(-diff < TimeSpan.FromHours(1));

				double db = r.GetDouble(4);
				Assert.AreEqual(p4_val, db);

				d = r.GetDateTime(5);
				Assert.AreEqual(DateTime.MaxValue, d);

				d = r.GetDateTime(6);
				Assert.AreEqual(DateTime.MinValue, d);

				d = r.GetDateTime(7);
				Assert.AreEqual(DateTime.MaxValue, d);

				d = r.GetDateTime(8);
				Assert.AreEqual(DateTime.MinValue, d);
			}

			next = r.NextResult();
			Assert.IsTrue(next);

			// select 123.456::numeric, :p3::text 
			while (r.Read())
			{
				decimal dec = r.GetDecimal(0);
				Assert.AreEqual(123.456M, dec);

				string str = r.GetString(1);
				Assert.AreEqual(p3_val.ToString(), str);
			}

			t.Commit();

			next = r.NextResult();
			Assert.IsTrue(next);

			// select 'NaN'::numeric, '-1234567898765432123456789.87654321'::numeric 
			while (r.Read())
			{
				double dou = r.GetDouble(0);
				Assert.AreEqual(Double.NaN, dou);

				dou = r.GetDouble(1);
				Assert.AreEqual(decimal.ToDouble(-1234567898765432123456789.87654321M), dou);
			}

			next = r.NextResult();
			Assert.IsTrue(next);

			int comp = 1;

			// select generate_series(1,1000000),generate_series(1,1000000)
			while (r.Read())
			{
				int gen = r.GetInt32(0);
				Assert.AreEqual(comp++, gen);
			}

			next = r.NextResult();
			Assert.IsTrue(next);

			comp = 1;

			// select generate_series(1,1000000),generate_series(1,1000000)
			while (r.Read())
			{
				int gen = r.GetInt32(0);
				Assert.AreEqual(comp++, gen);
			}

			Assert.AreEqual(1000001, comp);

			next = r.NextResult();
			Assert.IsTrue(next);

			comp = 0;

			// select :p1,:p2::text
			while (r.Read())
			{
				int i = r.GetInt32(0);
				string st = r.GetString(1);

				Assert.AreEqual(p1_val, i);
				string p2s = Convert.ToString(p2_val);
				Assert.AreEqual(p2s, st);
				comp++;
			}

			Assert.AreEqual(1, comp);

			next = r.NextResult();
			Assert.IsTrue(next);

			comp = 0;

			// select extract(epoch from date_trunc('day',current_date - :p9 ))::integer
			while (r.Read())
			{
				int i = r.GetInt32(0);

				int unix = (int) DateTime.UtcNow.Subtract(new TimeSpan(p9_val, 0, 0, 0)).Date.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

				Assert.AreEqual(unix, i);
				comp++;
			}

			next = r.NextResult();
			Assert.IsFalse(next);

			r.Close();
			r.Dispose();
		}

		[TestMethod]
		public void PqsqlDataReaderTest7()
		{
			// test UTF-8 column names roundtrip
			mCmd.CommandText = "select 12345 as \"€₺£$¥\", 67890 as \"⣿⣶⣤⣀⠀\";";

			using (PqsqlDataReader reader = mCmd.ExecuteReader())
			{
				reader.Read();

				string c0 = reader.GetName(0);
				string c1 = reader.GetName(1);

				Assert.AreEqual("€₺£$¥", c0);
				Assert.AreEqual("⣿⣶⣤⣀⠀", c1);
			}

			// test lower/upper parameter names roundtrip
			mCmd.CommandText = "select :P0 as \"零\", :P1 as \"一\", :p2 as \"二\", :p3 as \"三\" ;";
			mCmd.Parameters.AddWithValue("p0", 0);
			mCmd.Parameters.AddWithValue("p1", 1);
			mCmd.Parameters.AddWithValue("P2", 2);
			mCmd.Parameters.AddWithValue("P3", 3);

			using (PqsqlDataReader reader = mCmd.ExecuteReader())
			{
				reader.Read();

				string c0 = reader.GetName(0);
				string c1 = reader.GetName(1);
				string c2 = reader.GetName(2);
				string c3 = reader.GetName(3);

				int i0 = reader.GetInt32(0);
				int i1 = reader.GetInt32(1);
				int i2 = reader.GetInt32(2);
				int i3 = reader.GetInt32(3);

				Assert.AreEqual(0, i0);
				Assert.AreEqual(1, i1);
				Assert.AreEqual(2, i2);
				Assert.AreEqual(3, i3);

				Assert.AreEqual("零", c0);
				Assert.AreEqual("一", c1);
				Assert.AreEqual("二", c2);
				Assert.AreEqual("三", c3);
			}
		}

		[TestMethod]
		public void PqsqlDataReaderTest8()
		{
			const string qs = "select ARRAY[0,1,2,3,42,null,4711,-1]::int2[], :p1";

			short?[] p1_arr = { 0, 1, 2, 3, 42, null, 4711, -1 };

			mCmd.CommandText = qs;
			mCmd.CommandTimeout = 5;

			PqsqlParameter p1 = mCmd.CreateParameter();
			p1.ParameterName = "p1";
			p1.Value = p1_arr;
			p1.PqsqlDbType = PqsqlDbType.Array | PqsqlDbType.Int2;

			mCmd.Parameters.Add(p1);

			using (PqsqlDataReader r = mCmd.ExecuteReader())
			{
				// select ARRAY[0,1,2,3,42,null,4711,-1]::_int2, :p1
				bool read = r.Read();
				Assert.IsTrue(read);

				Array a0 = (Array) r.GetValue(0);
				Array a1 = (Array) r.GetValue(1);

				Assert.AreEqual(a0.Length, a1.Length);
				Assert.AreEqual(a0.Rank, a1.Rank);

				IEnumerator e1 = a1.GetEnumerator();

				foreach (object o0 in a0)
				{
					if (e1.MoveNext())
					{
						short? s0 = (short?) o0;
						short? s1 = (short?) e1.Current;
						Assert.AreEqual(s0, s1);
					}
					else
					{
						Assert.Fail("cannot advance a1");
					}
				}

				Assert.AreEqual((short)0, a0.GetValue(1));
				Assert.AreEqual((short)1, a0.GetValue(2));
				Assert.AreEqual((short)2, a0.GetValue(3));
				Assert.AreEqual((short)3, a0.GetValue(4));
				Assert.AreEqual((short)42, a0.GetValue(5));
				Assert.AreEqual(null, a0.GetValue(6));
				Assert.AreEqual((short)4711, a0.GetValue(7));
				Assert.AreEqual((short)-1, a0.GetValue(8));

				Assert.AreEqual((short)0, a1.GetValue(1));
				Assert.AreEqual((short)1, a1.GetValue(2));
				Assert.AreEqual((short)2, a1.GetValue(3));
				Assert.AreEqual((short)3, a1.GetValue(4));
				Assert.AreEqual((short)42, a1.GetValue(5));
				Assert.AreEqual(null, a1.GetValue(6));
				Assert.AreEqual((short)4711, a1.GetValue(7));
				Assert.AreEqual((short)-1, a1.GetValue(8));

				read = r.Read();
				Assert.IsFalse(read);
			}
		}

		[TestMethod]
		public void PqsqlDataReaderTest9()
		{
			mCmd.CommandText = @"select 1234567890123456789::int8, 0::oid, '00000000-0000-0000-0000-000000000000'::uuid, 47.11::float4, E'\\000\\001\\002\\003'::bytea, null as ""lastcol""";

			using (PqsqlDataReader reader = mCmd.ExecuteReader())
			{
				Assert.IsTrue(reader.HasRows);

				reader.Read();

				long c0 = reader.GetInt64(0);
				uint c1 = reader.GetOid(1);
				Guid c2 = reader.GetGuid(2);
				float c3 = reader.GetFloat(3);

				long len = reader.GetBytes(4, 0, null, 0, 0);
				Assert.AreEqual((long)4, len);
				byte[] c4 = new byte[len];
				long read = reader.GetBytes(4, 0, c4, 0, (int)len);
				Assert.AreEqual(read, len);

				Assert.AreEqual(1234567890123456789, c0);
				Assert.AreEqual((uint)0, c1);
				Assert.AreEqual(Guid.Empty, c2);
				Assert.AreEqual((float)47.11, c3);

				byte[] buf = {0, 1, 2, 3};
				Assert.AreEqual(buf[0], c4[0]);
				Assert.AreEqual(buf[1], c4[1]);
				Assert.AreEqual(buf[2], c4[2]);
				Assert.AreEqual(buf[3], c4[3]);

				object last = reader[5];
				Assert.AreEqual(DBNull.Value, last);

				last = reader["lastcol"];
				Assert.AreEqual(DBNull.Value, last);

				reader.Close();
			}
		}

		[TestMethod]
		public void PqsqlDataReaderTest10()
		{
			mCmd.CommandText = @"select timestamp 'now', date 'now', time 'now';";

			TimeSpan time = DateTime.UtcNow.TimeOfDay;
			using (PqsqlDataReader reader = mCmd.ExecuteReader())
			{
				Assert.IsTrue(reader.HasRows);

				reader.Read();

				DateTime c0 = reader.GetDateTime(0);
				DateTime c1 = reader.GetDateTime(1);
				DateTime c2 = reader.GetDateTime(2);

				Assert.AreEqual(DateTime.Today, c0 - c0.TimeOfDay);
				Assert.AreEqual(DateTime.Today, c1);

				// only compare hours and minutes
				Assert.AreEqual(time.Hours, c2.TimeOfDay.Hours);
				Assert.AreEqual(time.Minutes, c2.TimeOfDay.Minutes);

				reader.Close();
			}
		}

		[TestMethod]
		public void PqsqlDataReaderTest11()
		{
			mCmd.CommandText = @"select null, ''::varchar, '你', '好'::text, '吗'::char(1), 'x'::""char"" ;";

			using (PqsqlDataReader reader = mCmd.ExecuteReader())
			{
				Assert.IsTrue(reader.HasRows);

				reader.Read(); 

				char c1 = reader.GetChar(1);
				string s1 = reader.GetString(1);
				char c2 = reader.GetChar(2);
				string s2 = reader.GetString(2);
				char c3 = reader.GetChar(3);
				string s3 = reader.GetString(3);
				char c4 = reader.GetChar(4);
				string s4 = reader.GetString(4);
				char c5 = reader.GetChar(5);
				string s5 = reader.GetString(5);

				Assert.IsTrue(reader.IsDBNull(0));
				Assert.AreEqual(default(char), c1);
				Assert.AreEqual(string.Empty, s1);
				Assert.AreEqual('你', c2);
				Assert.AreEqual("你", s2);
				Assert.AreEqual('好', c3);
				Assert.AreEqual("好", s3);
				Assert.AreEqual('吗', c4);
				Assert.AreEqual("吗", s4);
				Assert.AreEqual('x', c5);
				Assert.AreEqual("x", s5);

				reader.Close();
			}
		}


		[TestMethod]
		public void PqsqlDataReaderTest12()
		{
			mCmd.CommandText = @"select interval '23 days', timestamp '2038-01-01', timestamptz '2038-01-01 00:00:00+02', date '2038-01-01', time '00:00:00', timetz '23:23:23+02';";

			using (PqsqlDataReader reader = mCmd.ExecuteReader())
			{
				Assert.IsTrue(reader.HasRows);

				reader.Read();

				TimeSpan ts0 = reader.GetTimeSpan(0);
				Assert.AreEqual(TimeSpan.FromDays(23), ts0);

				TimeSpan ts1 = reader.GetTimeSpan(1);
				DateTime dt1 = reader.GetDateTime(1);
				Assert.AreEqual(new DateTime(2038, 01, 01, 0, 0, 0), dt1);
				DateTimeOffset dto1 = reader.GetDateTimeOffset(1);
				Assert.AreEqual(new DateTimeOffset(2038,01,01,0,0,0,TimeSpan.Zero), dto1);

				TimeSpan ts2 = reader.GetTimeSpan(2);
				DateTime dt2 = reader.GetDateTime(2);
				Assert.AreEqual(new DateTime(2037, 12, 31, 22, 0, 0), dt2);
				DateTimeOffset dto2 = reader.GetDateTimeOffset(2);
				Assert.AreEqual(new DateTimeOffset(2038, 01, 01, 0, 0, 0, TimeSpan.FromHours(2)), dto2);

				TimeSpan ts3 = reader.GetTimeSpan(3);
				DateTime dt3 = reader.GetDateTime(3);
				Assert.AreEqual(new DateTime(2038, 01, 01, 0, 0, 0), dt3);
				DateTimeOffset dto3 = reader.GetDateTimeOffset(3);
				Assert.AreEqual(new DateTimeOffset(2038, 01, 01, 0, 0, 0, TimeSpan.Zero), dto3);

				TimeSpan ts4 = reader.GetTimeSpan(4);
				Assert.AreEqual(TimeSpan.FromSeconds(0), ts4);
				DateTime dt4 = reader.GetDateTime(4);
				Assert.AreEqual(DateTime.MinValue, dt4);
				DateTimeOffset dto4 = reader.GetDateTimeOffset(4);
				Assert.AreEqual(DateTimeOffset.MinValue, dto4);

				TimeSpan ts5 = reader.GetTimeSpan(5);
				//Assert.AreEqual(TimeSpan.FromHours(21).Add(TimeSpan.FromMinutes(23)).Add(TimeSpan.FromSeconds(23)), ts5);
				DateTime dt5 = reader.GetDateTime(5);
				//Assert.AreEqual(DateTime.MinValue.AddHours(21).AddMinutes(23).AddSeconds(23), dt5);
				DateTimeOffset dto5 = reader.GetDateTimeOffset(5);
				//Assert.AreEqual(new DateTimeOffset(DateTime.MinValue.AddHours(23).AddMinutes(23).AddSeconds(23), TimeSpan.FromHours(2)), dto5);

				reader.Close();
			}
		}
	}
}