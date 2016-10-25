using System.Data.Common;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlProviderFactoryTests
	{
		[TestMethod]
		public void PqsqlProviderFactoryTest1()
		{
			int version = PqsqlProviderFactory.ClientVersion;
			Assert.IsTrue(version >= 90400);

			int major = version/10000;
			version = version - major*10000;
			int minor = version/100;
			int revision = version - minor*100;

			Assert.IsTrue(major >= 9);
			Assert.IsTrue((major == 9 && minor >= 4) || major > 9);
			Assert.IsTrue(minor >= 0 && minor < 100 && revision >= 0 && revision < 100);

			DbConnectionStringBuilder connbuilder = PqsqlProviderFactory.Instance.CreateConnectionStringBuilder();
			Assert.IsNotNull(connbuilder);
			
			DbConnection conn = PqsqlProviderFactory.Instance.CreateConnection();
			Assert.IsNotNull(conn);

			DbCommand cmd = PqsqlProviderFactory.Instance.CreateCommand();
			Assert.IsNotNull(cmd);
			
			DbParameter par = PqsqlProviderFactory.Instance.CreateParameter();
			Assert.IsNotNull(par);

#if !DNXCORE50
			DbDataAdapter dba = PqsqlProviderFactory.Instance.CreateDataAdapter();
			Assert.IsNotNull(dba);
			
			DbCommandBuilder cmdbuilder = PqsqlProviderFactory.Instance.CreateCommandBuilder();
			Assert.IsNotNull(cmdbuilder);
#endif

		}
	}
}
