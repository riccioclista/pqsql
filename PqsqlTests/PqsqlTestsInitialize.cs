using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

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
