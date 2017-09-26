using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PqsqlTests
{
	/// <summary>
	/// setup PATH for libpq.dll assembly resolution
	/// </summary>
	[TestClass]
	public class PqsqlTestsInitialize
	{
		[AssemblyInitialize]
		public static void Initialize(TestContext context)
		{
			string libpqPath = context.Properties["libpqPath"].ToString();
			string path = Environment.GetEnvironmentVariable("path");
			Environment.SetEnvironmentVariable("path", libpqPath + ";" + path, EnvironmentVariableTarget.Process);
		}
	}
}
