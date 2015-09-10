using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace Pqsql
{
	public class PqsqlConnectionStringBuilder : DbConnectionStringBuilder
	{

		#region Dispose

		public virtual void Dispose()
		{
		}

		protected override void Dispose(bool disposing)
		{
		}

		#endregion
	}
}
