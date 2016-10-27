using System;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlConnectionStringBuilderTests
	{

		[TestMethod]
		public void PqsqlConnectionStringBuilderTest1()
		{
			PqsqlConnectionStringBuilder builder = new PqsqlConnectionStringBuilder("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");
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
	}
}
