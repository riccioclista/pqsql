using System;
using System.Data.Common;

namespace Pqsql
{
	public sealed class PqsqlException : DbException
	{
		public PqsqlException(string message)
			: base(message)
		{	}

		public PqsqlException(string message, Exception innerException)
			: base(message, innerException)
		{ }

		public PqsqlException(string message, int errorCode)
			: base(message, errorCode)
		{ }
	}
}
