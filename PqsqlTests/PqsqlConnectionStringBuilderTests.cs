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

			using (PqsqlConnection connection = new PqsqlConnection(builder))
			using (PqsqlCommand cmd = new PqsqlCommand("show all", connection))
			using (PqsqlDataReader r = cmd.ExecuteReader())
			{
				//
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
