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
	}
}
