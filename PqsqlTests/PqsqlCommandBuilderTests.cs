using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlCommandBuilderTests
	{
		[TestMethod]
		public void PqsqlCommandBuilderTest1()
		{
			PqsqlCommandBuilder builder = new PqsqlCommandBuilder();

			string qid = builder.QuoteIdentifier("a\"bc");
			Assert.AreEqual("\"a\"\"bc\"", qid, "wrong QuoteIdentifier");

			builder.QuotePrefix = null;
			builder.QuoteSuffix = null;

			qid = builder.QuoteIdentifier("a\"bc");
			Assert.AreEqual("a\"bc", qid, "wrong QuoteIdentifier");
		}

		[TestMethod]
		public void PqsqlCommandBuilderTest2()
		{
			using (PqsqlConnection connection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3"))
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
	}
}
