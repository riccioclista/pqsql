using System;
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

			Assert.AreEqual(7, dt.Rows.Count);

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

			Assert.AreEqual("waiting", dt.Rows[5][SchemaTableColumn.ColumnName]);
			Assert.AreEqual(6, dt.Rows[5][SchemaTableColumn.ColumnOrdinal]);

			Assert.AreEqual("query", dt.Rows[6][SchemaTableColumn.ColumnName]);
			Assert.AreEqual(7, dt.Rows[6][SchemaTableColumn.ColumnOrdinal]);

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

		[TestMethod]
		public void PqsqlDataReaderTest3()
		{
			mCmd.CommandText = "select datid,datname,pid,application_name,backend_start,waiting,query from pg_stat_activity";
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
			Assert.AreEqual("postgres", row[SchemaTableOptionalColumn.BaseCatalogName]);
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
			const string qs = "select ARRAY[0,1,2,3,42,null,4711]::_text, :p7, unnest(:p6), unnest(:p8) ;" +
												" select interval '20 days', interval '123 secs', interval '20 years 10 months', now(), :p4;" +
												" select 123.456::numeric, :p3::text ;" +
												" select 'NaN'::numeric, '-1234567898765432123456789.87654321'::numeric ;" +
												" select generate_series(1,10000000),generate_series(1,10000000);" +
												" select generate_series(1,10000000),generate_series(1,10000000);" +
												" select :p1,:p2::text ;" +
												" select extract(epoch from date_trunc('day',current_date - :p9 ))::integer ";

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
			p9.Value = 47;
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

			// select interval '20 days', interval '123 secs', interval '20 years 10 months', now(), :p4
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

			// select generate_series(1,10000000),generate_series(1,10000000)
			while (r.Read())
			{
				int gen = r.GetInt32(0);
				Assert.AreEqual(comp++, gen);
			}

			next = r.NextResult();
			Assert.IsTrue(next);

			comp = 1;

			// select generate_series(1,10000000),generate_series(1,10000000)
			while (r.Read())
			{
				int gen = r.GetInt32(0);
				Assert.AreEqual(comp++, gen);
			}


			// select :p1,:p2::text
			// select extract(epoch from date_trunc('day',current_date - :p9 ))::integer
			while (r.NextResult())
			{
				int n1 = 0;

				while (r.Read())
				{
					int i = r.GetInt32(0);
					n1++;
				}

				Assert.AreEqual(1, n1);
			}
		}

		[TestMethod]
		public void PqsqlDataReaderTest7()
		{
			// test UTF-8 column names roundtrip
			mCmd.CommandText = "select 12345 as \"€₺£$¥\", 67890 as \"⣿⣶⣤⣀⠀\"";

			PqsqlDataReader reader = mCmd.ExecuteReader();

			reader.Read();

			string c0 = reader.GetName(0);
			string c1 = reader.GetName(1);

			Assert.AreEqual("€₺£$¥", c0);
			Assert.AreEqual("⣿⣶⣤⣀⠀", c1);

			reader.Close();
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

	}
}