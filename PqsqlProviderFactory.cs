using System.Data.Common;

namespace Pqsql
{
	public class PqsqlProviderFactory : DbProviderFactory
	{
		public static PqsqlProviderFactory Instance = new PqsqlProviderFactory();

		private PqsqlProviderFactory()
    {
    }

    public override DbCommand CreateCommand()
    {
			return new PqsqlCommand();
    }

    public override DbConnection CreateConnection()
    {
      return new PqsqlConnection();
    }

		public override DbParameter CreateParameter()
		{
			return new PqsqlParameter();
		}

		public override DbConnectionStringBuilder CreateConnectionStringBuilder()
		{
			return new PqsqlConnectionStringBuilder();
		}

#if !DNXCORE50
		public override DbCommandBuilder CreateCommandBuilder()
		{
			return new PqsqlCommandBuilder();
		}

		public override DbDataAdapter CreateDataAdapter()
		{
			return new PqsqlDataAdapter();
		}
#endif

	}
}
