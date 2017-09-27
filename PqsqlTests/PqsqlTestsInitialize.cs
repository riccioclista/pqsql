using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PqsqlTests
{
	/// <summary>
	/// setup PATH for libpq.dll assembly resolution
	/// and PGSERVICEFILE for pg_service.conf
	/// </summary>
	[TestClass]
	public class PqsqlTestsInitialize
	{
		[AssemblyInitialize]
		public static void Initialize(TestContext context)
		{
			// https://msdn.microsoft.com/en-us/library/ms682586.aspx
			object libpqPath = context.Properties["libpqPath"];
			if (libpqPath != null)
			{
				string path = Environment.GetEnvironmentVariable("path");
				Environment.SetEnvironmentVariable("path", libpqPath + ";" + path, EnvironmentVariableTarget.Process);
			}

			// https://www.postgresql.org/docs/current/static/libpq-pgservice.html
			object pgServiceFile = context.Properties["pgServiceFile"];
			if (pgServiceFile != null)
			{
				Environment.SetEnvironmentVariable("PGSERVICEFILE", pgServiceFile.ToString(), EnvironmentVariableTarget.Process);
			}
		}
	}
}
