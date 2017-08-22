using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlCommandBuilderTests
	{
		private static string connectionString = string.Empty;

		#region Additional test attributes

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			connectionString = context.Properties["connectionString"].ToString();
		}

		[TestInitialize]
		public void TestInitialize()
		{
		}

		[TestCleanup]
		public void TestCleanup()
		{
		}

		#endregion

		[TestMethod]
		public void PqsqlCommandBuilderTest1()
		{
			PqsqlCommandBuilder builder = new PqsqlCommandBuilder();

			Assert.AreEqual(builder.QuotePrefix, "\"");
			Assert.AreEqual(builder.QuoteSuffix, "\"");

			string qid = builder.QuoteIdentifier("a\"bc");
			Assert.AreEqual("\"a\"\"bc\"", qid, "wrong QuoteIdentifier");

			builder.QuotePrefix = null;
			builder.QuoteSuffix = null;

			qid = builder.QuoteIdentifier("a\"bc");
			Assert.AreEqual("a\"bc", qid, "wrong QuoteIdentifier");

			builder.QuotePrefix = "\"";
			builder.QuoteSuffix = "\"";

			string uqid = builder.UnquoteIdentifier("\"a\"\"bc\"");
			Assert.AreEqual(qid, uqid, "wrong UnquoteIdentifier");
		}

		[TestMethod]
		public void PqsqlCommandBuilderTest2()
		{
			using (PqsqlConnection connection = new PqsqlConnection(connectionString))
			using (PqsqlCommand command = connection.CreateCommand())
			{
				PqsqlTransaction transaction = connection.BeginTransaction();
				command.Transaction = transaction;
				command.CommandText = "create temp table temptab (c0 int4 primary key, c1 float8)";
				command.CommandType = CommandType.Text;
				command.ExecuteNonQuery();
				transaction.Commit(); // temp table must be visible in the next transaction

				transaction = connection.BeginTransaction();

				PqsqlDataAdapter adapter = new PqsqlDataAdapter("select * from temptab", connection)
				{
					SelectCommand =
					{
						Transaction = transaction
					},
				};
				
				adapter.RowUpdated += Adapter_RowUpdated;

				PqsqlCommandBuilder builder = new PqsqlCommandBuilder(adapter);

				DataSet ds = new DataSet();
				adapter.FillSchema(ds, SchemaType.Source);
				adapter.Fill(ds, "temptab");

				DataTable temptab = ds.Tables["temptab"];
				DataRow row = temptab.NewRow();
				row["c0"] = 123;
				row["c1"] = 1.23;
				temptab.Rows.Add(row);

				adapter.Update(ds, "temptab");

				command.CommandText = "select * from temptab";
				command.CommandType = CommandType.Text;

				using (PqsqlDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						Assert.AreEqual(123, reader.GetInt32(0));
						Assert.AreEqual(1.23, reader.GetDouble(1));
					}
				}

				transaction.Rollback();
			}
		}

		private void Adapter_RowUpdated(object sender, RowUpdatedEventArgs e)
		{
			Assert.AreEqual(UpdateStatus.Continue, e.Status);
		}

		[TestMethod]
		public void PqsqlCommandBuilderTest3()
		{
			using (PqsqlConnection connection = new PqsqlConnection(connectionString))
			using (PqsqlCommand command = connection.CreateCommand())
			{
				PqsqlTransaction transaction = connection.BeginTransaction();
				command.Transaction = transaction;
				command.CommandText = "create temp table temptab (c0 int4 primary key, c1 float8)";
				command.CommandType = CommandType.Text;
				command.ExecuteNonQuery();
				transaction.Commit(); // temp table must be visible in the next transaction

				transaction = connection.BeginTransaction();

				PqsqlDataAdapter adapter = new PqsqlDataAdapter("select * from temptab", connection)
				{
					SelectCommand =
					{
						Transaction = transaction
					}
				};
				PqsqlCommandBuilder builder = new PqsqlCommandBuilder(adapter);

				// INSERT INTO "postgres"."pg_temp_2"."temptab" ("c0", "c1") VALUES (:p1, :p2)
				PqsqlCommand inserter = builder.GetInsertCommand();
				inserter.Parameters["p1"].Value = 1;
				inserter.Parameters["p2"].Value = 2.1;
				int inserted = inserter.ExecuteNonQuery();
				Assert.AreEqual(1, inserted);

				// UPDATE "postgres"."pg_temp_2"."temptab"
				// SET "c0" = :p1, "c1" = :p2
				// WHERE (("c0" = :p3) AND ((:p4 = 1 AND "c1" IS NULL) OR ("c1" = :p5)))
				PqsqlCommand updater = builder.GetUpdateCommand();
				updater.Parameters["p1"].Value = 2;
				updater.Parameters["p2"].Value = 2.2;
				updater.Parameters["p3"].Value = 1;
				updater.Parameters["p4"].Value = 0;
				updater.Parameters["p5"].Value = 2.1;
				int updated = updater.ExecuteNonQuery();
				Assert.AreEqual(1, updated);

				// DELETE FROM "postgres"."pg_temp_2"."temptab"
				// WHERE (("c0" = :p1) AND ((:p2 = 1 AND "c1" IS NULL) OR ("c1" = :p3)))
				PqsqlCommand deleter = builder.GetDeleteCommand();
				deleter.Parameters["p1"].Value = 2;
				deleter.Parameters["p2"].Value = 0;
				deleter.Parameters["p3"].Value = 2.2;
				int deleted = deleter.ExecuteNonQuery();
				Assert.AreEqual(1, deleted);

				transaction.Rollback();
			}
		}
	}
}
