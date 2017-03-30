using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlParameterCollectionTests
	{
		[TestMethod]
		public void PqsqlParameterCollectionTest1()
		{
			PqsqlParameterCollection coll = new PqsqlParameterCollection();
			Assert.IsFalse(coll.Contains("p1"));

			coll.AddRange(new PqsqlParameter[] { new PqsqlParameter("p1", DbType.Int32), });
			Assert.IsTrue(coll.Contains("p1"));
			Assert.AreEqual(1, coll.Count);

			int i = coll.IndexOf("p1");
			Assert.AreEqual(0, i);

			coll.RemoveAt(i);
			Assert.IsFalse(coll.Contains("p1"));
		}

		[TestMethod]
		public void PqsqlParameterCollectionTest2()
		{
			PqsqlParameterCollection coll = new PqsqlParameterCollection();
			IntPtr pb1 = coll.CreateParameterBuffer();
			Assert.AreNotEqual(IntPtr.Zero, pb1);

			coll.AddRange(
				new PqsqlParameter[]
				{
					new PqsqlParameter("p1", DbType.Int32),
					new PqsqlParameter("p2", DbType.Boolean) { Direction = ParameterDirection.InputOutput },
					new PqsqlParameter("p3", DbType.Double) { Direction = ParameterDirection.Output },
				}
				);

			IntPtr pb2 = coll.CreateParameterBuffer();
			Assert.AreEqual(pb1, pb2);
		}

		[TestMethod]
		public void PqsqlParameterCollectionTest3()
		{
			PqsqlParameterCollection coll = new PqsqlParameterCollection();

			Array parameters = new PqsqlParameter[]
			{
				new PqsqlParameter("p1", DbType.Int32),
				new PqsqlParameter("p2", DbType.Boolean) { Direction = ParameterDirection.InputOutput },
				new PqsqlParameter("p3", DbType.Double) { Direction = ParameterDirection.Output },
			};
			coll.AddRange(parameters);
			Assert.AreEqual(3, coll.Count);

			Array copy = new PqsqlParameter[3];
			coll.CopyTo(copy, 0);

			Assert.AreEqual(parameters.Length, copy.Length);

			coll.Remove(copy.GetValue(1));
			Assert.AreEqual(2, coll.Count);

			bool keynotfound = false;
			try
			{
				coll.RemoveAt("p2");
			}
			catch (KeyNotFoundException)
			{
				keynotfound = true;
			}
			finally
			{

				Assert.IsTrue(keynotfound);
			}
			
			coll.Insert(coll.IndexOf("p3"), new PqsqlParameter("p4", DbType.Int64));
			Assert.AreEqual(3, coll.Count);

			coll.RemoveAt("p3");
			Assert.AreEqual(2, coll.Count);

			coll.Clear();
			Assert.AreEqual(0, coll.Count);
		}
	}
}
