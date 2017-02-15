using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlCommandTests
	{
		private PqsqlConnection mConnection;

		#region Additional test attributes

		[TestInitialize]
		public void MyTestInitialize()
		{
			mConnection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");
		}

		[TestCleanup]
		public void MyTestCleanup()
		{
			mConnection.Dispose();
		}

		#endregion

		[TestMethod]
		public void PqsqlCommandTest1()
		{
			PqsqlCommand cmd = mConnection.CreateCommand();
			cmd.CommandText = "select pg_sleep(3);";
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
			cmd.CommandText = "create or replace function test_out(p1 out text, i1 inout int, p2 out int, r inout refcursor) as $$begin $1 := 'p1 text'; $2:=$2*-4711; $3:=12345; open r for select * from ( values (1,2,3),(4,5,6),(7,8,9) ) X; end;$$ LANGUAGE plpgsql;";
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

			cmd.CommandText = "test_out";
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
	}
}
