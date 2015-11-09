using System;
using System.Data.Common;

namespace Pqsql
{
	public sealed class PqsqlDataAdapter : DbDataAdapter
	{

		public PqsqlDataAdapter()
		{
		}

		public PqsqlDataAdapter(PqsqlCommand selectCommand)
    {
      SelectCommand = selectCommand;
    }

		public PqsqlDataAdapter(String selectCommandText, PqsqlConnection selectConnection)
			: this(new PqsqlCommand(selectCommandText, selectConnection))
    {
		}

		public PqsqlDataAdapter(String selectCommandText, String selectConnectionString)
			: this(selectCommandText, new PqsqlConnection(selectConnectionString))
    {
		}

	}
}
