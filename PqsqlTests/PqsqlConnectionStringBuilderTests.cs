using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlConnectionStringBuilderTests
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
		public void PqsqlConnectionStringBuilderTest1()
		{
			PqsqlConnectionStringBuilder builder = new PqsqlConnectionStringBuilder(connectionString);
			builder[PqsqlConnectionStringBuilder.keepalives] = "1";
			builder[PqsqlConnectionStringBuilder.keepalives_idle] = "23";
			builder[PqsqlConnectionStringBuilder.keepalives_count] = "3";
			builder[PqsqlConnectionStringBuilder.keepalives_interval] = "3";

			string dataSource;

			using (PqsqlConnection connection = new PqsqlConnection(builder))
			{
				// closed connection with service file should give us empty data source
				Assert.IsTrue(string.IsNullOrEmpty(connection.DataSource));

				using (PqsqlCommand cmd = new PqsqlCommand("show all", connection))
				using (PqsqlDataReader r = cmd.ExecuteReader())
				{
					object value;
					if (builder.TryGetValue(PqsqlConnectionStringBuilder.host, out value))
					{
						Assert.AreEqual(connection.DataSource, value);
						dataSource = value.ToString();
					}
					else // no datasource specified
					{
						dataSource = connection.DataSource;
					}
					cmd.Cancel();
				}
			}

			builder[PqsqlConnectionStringBuilder.host] = dataSource;

			using (PqsqlConnection connection = new PqsqlConnection(builder))
			using (PqsqlCommand cmd = new PqsqlCommand("show all", connection))
			using (PqsqlDataReader r = cmd.ExecuteReader())
			{
				object value;
				if (builder.TryGetValue(PqsqlConnectionStringBuilder.host, out value))
				{
					Assert.AreEqual(connection.DataSource, value);
				}
				else
				{
					Assert.Fail("host part is not available");
				}
				cmd.Cancel();
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException), "null exception should have been given")]
		public void PqsqlConnectionStringBuilderTest2()
		{
			PqsqlConnectionStringBuilder builder = new PqsqlConnectionStringBuilder(null);
			Assert.Fail();
		}

		[TestMethod]
		public void PqsqlConnectionStringBuilderTest3()
		{
			PqsqlConnectionStringBuilder builder = new PqsqlConnectionStringBuilder(string.Empty);

			Assert.AreEqual(string.Empty, builder.ConnectionString);
			Assert.AreEqual(0, builder.Count);

			using (PqsqlConnection connection = new PqsqlConnection(builder))
			{
				try
				{
					connection.Open();
				}
				catch (PqsqlException)
				{
					// ignored, depends on server config whether empty connection string is valid
				}

				connection.Close();
			}
		}
	}
}
