using System;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlCommandTests
	{
		private static string connectionString = string.Empty;

		private PqsqlConnection mConnection;

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
		}

		[TestCleanup]
		public void TestCleanup()
		{
			mConnection.Dispose();
		}

		#endregion

		[TestMethod]
		public void PqsqlCommandTest1()
		{
			PqsqlCommand cmd = mConnection.CreateCommand();
			cmd.CommandText = "select pg_sleep(1);";
			PqsqlDataReader r = cmd.ExecuteReader();

			bool b = r.Read();
			Assert.AreEqual(true, b);

			object v = r.GetValue(0);
			Assert.AreEqual("", v);
		}

		[TestMethod]
		public void PqsqlCommandTest2()
		{
			PqsqlCommand cmd = mConnection.CreateCommand();
			cmd.CommandText = "bit_length";
			cmd.CommandType = CommandType.StoredProcedure;

			IDbDataParameter i = cmd.CreateParameter();
			i.ParameterName = "i";
			i.Value = "postgres";
			cmd.Parameters.Add(i);

			IDbDataParameter o = cmd.CreateParameter();
			o.ParameterName = "bit_length";
			o.Direction = ParameterDirection.Output;
			cmd.Parameters.Add(o);

			PqsqlDataReader r = cmd.ExecuteReader();

			bool b = r.Read();
			Assert.AreEqual(true, b);

			Assert.AreEqual(64, o.Value);
		}

		[TestMethod]
		[ExpectedException(typeof(PqsqlException), "unknown output parameter should have been given")]
		public void PqsqlCommandTest3()
		{
			PqsqlCommand cmd = mConnection.CreateCommand();
			cmd.CommandText = "upper";
			cmd.CommandType = CommandType.StoredProcedure;

			IDbDataParameter i = cmd.CreateParameter();
			i.ParameterName = "i";
			i.Value = "upper";
			cmd.Parameters.Add(i);

			IDbDataParameter o = cmd.CreateParameter();
			o.ParameterName = "output_parametername_does_not_exist";
			o.Direction = ParameterDirection.Output;
			cmd.Parameters.Add(o);

			cmd.ExecuteReader();

			Assert.Fail();
		}

		[TestMethod]
		public void PqsqlCommandTest4()
		{
			PqsqlCommand cmd = new PqsqlCommand("generate_series", mConnection);

			const int p1_val = -1;
			const int p2_val = 2;

			PqsqlParameter p1 = cmd.CreateParameter();
			p1.ParameterName = "p1";
			p1.Value = p1_val;
			p1.DbType = DbType.Int32;

			PqsqlParameter p2 = cmd.CreateParameter();
			p2.ParameterName = "p2";
			p2.Value = p2_val;
			p2.DbType = DbType.Int32;

			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(p1);
			cmd.Parameters.Add(p2);
			cmd.Parameters.Add(new PqsqlParameter("generate_series", DbType.Int32)
			{
				Direction = ParameterDirection.Output
			});

			PqsqlDataReader r = cmd.ExecuteReader();
			int n = p1_val - 1;
			int j = p1_val;
			while (r.Read())
			{
				int i = r.GetInt32(0);
				Assert.AreEqual(j++, i);
				n++;
			}
			Assert.AreEqual(p2_val, n);
		}

		[TestMethod]
		public void PqsqlCommandTest5()
		{
			PqsqlCommand cmd = mConnection.CreateCommand();
			cmd.CommandText = "pg_authid";
			cmd.CommandTimeout = 10;
			cmd.CommandType = CommandType.TableDirect;

			var r = cmd.ExecuteReader();

			foreach (var o in r)
			{
				Assert.IsNotNull(o);
			}

			cmd.Cancel();
			mConnection.Close();
			mConnection.Open();
		}


		[TestMethod]
		public void PqsqlCommandTest6()
		{
			PqsqlTransaction t = mConnection.BeginTransaction();

			PqsqlCommand cmd = mConnection.CreateCommand();

			cmd.Transaction = t;
			cmd.CommandText = "create or replace function pg_temp.test_out(p1 out text, i1 inout int, p2 out int, r inout refcursor) as $$begin $1 := 'p1 text'; $2:=$2*-4711; $3:=12345; open r for select * from ( values (1,2,3),(4,5,6),(7,8,9) ) X; end;$$ LANGUAGE plpgsql;";
			cmd.CommandTimeout = 10;
			cmd.CommandType = CommandType.Text;

			int	n = cmd.ExecuteNonQuery();
			Assert.AreEqual(0, n);

			PqsqlParameter p1 = new PqsqlParameter("p1", DbType.String)
			{
				Direction = ParameterDirection.Output,
				Value = "p1_val"
			};

			const int p2_val = 4711;
			PqsqlParameter p2 = new PqsqlParameter("i1", DbType.Int32, (object)p2_val)
			{
				Direction = ParameterDirection.InputOutput,
			};

			PqsqlParameter p3 = new PqsqlParameter("p2", DbType.Int32)
			{
				Direction = ParameterDirection.Output,
				Value = 42
			};

			const string p4_val = "portal_name"; 
			PqsqlParameter p4 = new PqsqlParameter
			{
				ParameterName = "r",
				PqsqlDbType = PqsqlDbType.Refcursor,
				Direction = ParameterDirection.InputOutput,
				Value = p4_val
			};

			cmd.CommandText = "pg_temp.test_out";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(p1);
			cmd.Parameters.Add(p2);
			cmd.Parameters.Add(p3);
			cmd.Parameters.Add(p4);

			n = cmd.ExecuteNonQuery();
			Assert.AreEqual(-1, n);
			Assert.AreEqual("p1 text", p1.Value);
			Assert.AreEqual(p2_val * -p2_val, p2.Value);
			Assert.AreEqual(12345, p3.Value);
			Assert.AreEqual(p4_val, p4.Value);

			cmd.CommandText = string.Format("fetch all from {0}", p4.Value);
			cmd.CommandType = CommandType.Text;

			PqsqlDataReader r = cmd.ExecuteReader();

			int i = 1;
			while (r.Read())
			{
				Assert.AreEqual(i++, r.GetValue(0));
				Assert.AreEqual(i++, r.GetValue(1));
				Assert.AreEqual(i++, r.GetValue(2));
			}

			t.Rollback();
		}

		[TestMethod]
		public void PqsqlCommandTest7()
		{
			PqsqlTransaction t = mConnection.BeginTransaction();

			PqsqlCommand cmd = mConnection.CreateCommand();

			cmd.Transaction = t;
			cmd.CommandText = "create temp table temptab (c0 int4 primary key, c1 float8); insert into temptab values (1,1.0); insert into temptab values (2,2.0); update temptab set c1 = 3.0 where c0 = 2;";
			cmd.CommandTimeout = 10;
			cmd.CommandType = CommandType.Text;

			int n = cmd.ExecuteNonQuery();

			t.Rollback();

			Assert.AreEqual(3, n);
		}

		[TestMethod]
		public void PqsqlCommandTest8()
		{
			PqsqlTransaction t = mConnection.BeginTransaction();

			PqsqlCommand cmd = mConnection.CreateCommand();

			cmd.Transaction = t;
			cmd.CommandText = " ; create temp table temptab (c0 int4); ; insert into temptab values (1); insert into temptab values (2); insert into temptab values (3);";
			cmd.CommandTimeout = 10;
			cmd.CommandType = CommandType.Text;

			int n = cmd.ExecuteNonQuery();

			t.Rollback();

			Assert.AreEqual(3, n);
		}


		[TestMethod]
		public void PqsqlCommandTest9()
		{
			PqsqlTransaction t = mConnection.BeginTransaction();

			PqsqlCommand cmd = mConnection.CreateCommand();

			cmd.Transaction = t;
			cmd.CommandText = @"create or replace function ""pg_temp"".""test me""(i int) returns int as $code$ begin return $1 * i; end; $code$ language plpgsql;
								select ""pg_temp"".""test me""(:p1);
								select ""pg_temp"".""test me""($1)";
			cmd.CommandTimeout = 2;
			cmd.CommandType = CommandType.Text;
			cmd.Parameters.AddWithValue("p1", 4711);

			using (PqsqlDataReader r = cmd.ExecuteReader())
			{
				bool good = r.Read();
				Assert.IsFalse(good);

				good = r.NextResult();
				Assert.IsTrue(good);

				good = r.Read();
				Assert.IsTrue(good);

				int n = r.GetInt32(0);
				Assert.AreEqual(4711 * 4711, n);

				good = r.Read();
				Assert.IsFalse(good);

				good = r.NextResult();
				Assert.IsTrue(good);

				good = r.Read();
				Assert.IsTrue(good);

				n = r.GetInt32(0);
				Assert.AreEqual(4711 * 4711, n);

				good = r.Read();
				Assert.IsFalse(good);
			}

			cmd.CommandText = "\"pg_temp\".\"test me\"";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Clear();
			cmd.Parameters.AddWithValue("i", 4711);
			cmd.Parameters.Add(new PqsqlParameter
			{
				ParameterName = "\"pg_temp\".\"test me\"",
				DbType = DbType.Int32,
				Direction = ParameterDirection.Output
			});

			object x = cmd.ExecuteScalar();
			Assert.AreEqual(4711 * 4711, x);

			t.Rollback();
		}

		[TestMethod]
		public void PqsqlCommandTest10()
		{
			StringBuilder sb = new StringBuilder();

			const int N = 1664; // postgres can handle at most 1664 columns in a select
			const int K = 5; // create K*(K+1) / 2 queries

			PqsqlParameter[] pars = new PqsqlParameter[0];

			for (int k = 1; k <= K; k++)
			{
				if (k > 1) sb.Append(';');
				sb.Append("select ");

				Array.Resize(ref pars, k * N);

				using (PqsqlTransaction t = mConnection.BeginTransaction())
				{
					for (int i = 1; i <= N; i++)
					{
						int j = (k - 1) * N + i - 1;

						if (i > 1) sb.Append(',');
						sb.Append("generate_series(:p" + j + ",:p" + j + ")");

						PqsqlParameter p = new PqsqlParameter
						{
							ParameterName = "p" + j,
							DbType = DbType.Int32,
							Value = j
						};
						pars[j] = p;
					}

					sb.Append(';');

					using (PqsqlCommand cmd = mConnection.CreateCommand())
					{
						cmd.Transaction = t;
						cmd.CommandText = sb.ToString();
						cmd.CommandTimeout = 20;
						cmd.CommandType = CommandType.Text;
						cmd.Parameters.AddRange(pars.Take(k*N).ToArray());

						using (PqsqlDataReader reader = cmd.ExecuteReader())
						{
							for (int n = 0; n < k; n++)
							{
								while (reader.Read())
								{
									for (int m = 0; m < N; m++)
									{
										int o = reader.GetInt32(m);
										Assert.AreEqual(n*N + m, o);
									}
								}
								reader.NextResult();
							}
						}
					}
				}
			}
		}

		[TestMethod]
		public void PqsqlCommandTest11()
		{
			PqsqlCommand cmd = new PqsqlCommand("select :p1;", mConnection);
			cmd.CommandType = CommandType.Text;

			// recursive parameters
			PqsqlParameter p1 = cmd.Parameters.AddWithValue(":p1", ":p1");

			PqsqlDataReader r = cmd.ExecuteReader();
			while (r.Read())
			{
				string s = r.GetString(0);
				Assert.AreEqual(p1.Value, s);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(PqsqlException), "syntax error should have been given")]
		public void PqsqlCommandTest12()
		{
			PqsqlCommand cmd = new PqsqlCommand("select :p1;", mConnection)
			{
				CommandType = CommandType.Text,
				Parameters =
				{
					// do not add parameter :p1, add p0 and p2 instead
					new PqsqlParameter(":p0", DbType.String, "p0value"),
					new PqsqlParameter(":p2", DbType.String, "p2value")
				}
			};

			using (PqsqlDataReader r = cmd.ExecuteReader())
			{
				Assert.Fail("ExecuteReader() must fail");
			}
		}

		[TestMethod]
		[ExpectedException(typeof(PqsqlException), "syntax error should have been given")]
		public void PqsqlCommandTest13()
		{
			PqsqlCommand cmd = new PqsqlCommand("select :貓1;", mConnection)
			{
				CommandType = CommandType.Text
			};

			// add non-standard parametername
			cmd.Parameters.AddWithValue(":貓1", "喵");

			using (PqsqlDataReader r = cmd.ExecuteReader())
			{
				Assert.Fail("ExecuteReader() must fail");
			}
		}

		[TestMethod]
		[ExpectedException(typeof(PqsqlException), "row is too big error should have been given")]
		public void PqsqlCommandTest14()
		{
			const int n = 600;
			PqsqlTransaction t = mConnection.BeginTransaction();

			StringBuilder sb = new StringBuilder("CREATE TEMP TABLE testcopy (c0 int4,");
			for (int i = 1; i < n; i++)
			{
				if (i > 1)
					sb.Append(',');
				sb.AppendFormat("c{0} text", i);
			}
			sb.Append(");");

			PqsqlCommand cmd = mConnection.CreateCommand();
			cmd.Transaction = t;
			cmd.CommandText = sb.ToString();
			cmd.CommandTimeout = 100;
			cmd.CommandType = CommandType.Text;

			cmd.ExecuteNonQuery();

			sb.Clear();
			sb.Append("INSERT INTO testcopy VALUES (:p0,");
			cmd.Parameters.AddWithValue(":p0", 0);

			for (int i = 1; i < n; i++)
			{
				if (i > 1)
					sb.Append(',');

				string par = ":p" + i;
				sb.AppendFormat(par);
				cmd.Parameters.AddWithValue(par, "01234567890123456789");
			}
			sb.Append(");");

			cmd.CommandText = sb.ToString();

			try
			{
				cmd.ExecuteNonQuery();
			}
			catch (PqsqlException e)
			{
				Assert.AreEqual((int)PqsqlState.PROGRAM_LIMIT_EXCEEDED, e.ErrorCode);
				throw;
			}

			Assert.Fail();
		}

		[TestMethod]
		public void PqsqlCommandTest15()
		{
			using (PqsqlCommand cmd =
				new PqsqlCommand("select state from pg_stat_activity --\n; /* select 1 */", mConnection) {CommandType = CommandType.Text})
			{
				PqsqlDataReader r = cmd.ExecuteReader();
				while (r.Read())
				{
					string s = !r.IsDBNull(0) ? r.GetString(0) : null;
				}
			}
		}

		[TestMethod]
		public void PqsqlCommandTest16()
		{
			using (PqsqlCommand cmd =
				new PqsqlCommand("select state from /* xxx */ pg_stat_activity\n;", mConnection) { CommandType = CommandType.Text })
			{
				PqsqlDataReader r = cmd.ExecuteReader();
				while (r.Read())
				{
					string s = !r.IsDBNull(0) ? r.GetString(0) : null;
				}
			}
		}

		[TestMethod]
		public void PqsqlCommandTest17()
		{
			using (PqsqlCommand cmd =
				new PqsqlCommand("select /*state*/'x' from /* xxx */ pg_stat_activity ", mConnection) { CommandType = CommandType.Text })
			{
				PqsqlDataReader r = cmd.ExecuteReader();
				while (r.Read())
				{
					string s = !r.IsDBNull(0) ? r.GetString(0) : null;
				}
			}
		}

		[TestMethod]
		public void PqsqlCommandTest18()
		{
			using (PqsqlCommand cmd =
				new PqsqlCommand("select state from pg_stat_activity -- forcing ;", mConnection) { CommandType = CommandType.Text })
			{
				PqsqlDataReader r = cmd.ExecuteReader();
				while (r.Read())
				{
					string s = !r.IsDBNull(0) ? r.GetString(0) : null;
				}
			}
		}

		[TestMethod]
		public void PqsqlCommandTest19()
		{
			using (PqsqlCommand cmd =
				new PqsqlCommand("select state from pg_stat_activity -- no semicolon", mConnection) { CommandType = CommandType.Text })
			{
				PqsqlDataReader r = cmd.ExecuteReader();
				while (r.Read())
				{
					string s = !r.IsDBNull(0) ? r.GetString(0) : null;
				}
			}
		}
	}
}
