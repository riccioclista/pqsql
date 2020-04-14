using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using System.IO;

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
				string path = Environment.GetEnvironmentVariable("PATH");
				Environment.SetEnvironmentVariable("PATH", $"{libpqPath}{Path.PathSeparator}{path}", EnvironmentVariableTarget.Process);
			}

			// https://www.postgresql.org/docs/current/static/libpq-pgservice.html
			object pgServiceFile = context.Properties["pgServiceFile"];
			if (pgServiceFile != null)
			{
#if WIN32
				Environment.SetEnvironmentVariable("PGSERVICEFILE", pgServiceFile.ToString(), EnvironmentVariableTarget.Process);
#else
				var result = setenv("PGSERVICEFILE", pgServiceFile.ToString());
#endif
			}
		}

#if !WIN32
		[DllImport("c")]
		public static extern int setenv(string name, string value);
#endif
	}
}
