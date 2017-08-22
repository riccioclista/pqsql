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

			coll.AddRange(
				new PqsqlParameter[]
				{
					new PqsqlParameter("p1", DbType.Int32),
					new PqsqlParameter("p2", DbType.Boolean) { Direction = ParameterDirection.InputOutput },
					new PqsqlParameter("p3", DbType.Double) { Direction = ParameterDirection.Output },
				}
				);

			Assert.AreEqual(3, coll.Count);

			coll.Clear();

			Assert.AreEqual(0, coll.Count);
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

		[TestMethod]
		public void PqsqlParameterCollectionTest4()
		{
			PqsqlParameterCollection coll = new PqsqlParameterCollection
			{
				new PqsqlParameter("p1", DbType.Int32)
			};

			Assert.AreEqual(1, coll.Count);
			Assert.AreEqual(":p1", coll[0].ParameterName);
			Assert.AreEqual(DbType.Int32, coll[0].DbType);
			Assert.AreEqual(ParameterDirection.Input, coll[0].Direction);

			coll["p1"] = new PqsqlParameter() { ParameterName = "p2", DbType = DbType.Int32, Direction = ParameterDirection.InputOutput};

			Assert.AreEqual(1, coll.Count);
			Assert.AreEqual(":p2", coll[0].ParameterName);
			Assert.AreEqual(DbType.Int32, coll[0].DbType);
			Assert.AreEqual(ParameterDirection.InputOutput, coll[0].Direction);

			coll["p2"] = new PqsqlParameter() { ParameterName = "p3", DbType = DbType.String };

			Assert.AreEqual(1, coll.Count);
			Assert.AreEqual(":p3", coll[0].ParameterName);
			Assert.AreEqual(DbType.String, coll[0].DbType);
			Assert.AreEqual(ParameterDirection.Input, coll[0].Direction);
		}

		[TestMethod]
		public void PqsqlParameterCollectionTest5()
		{
			PqsqlParameterCollection coll = new PqsqlParameterCollection();

			PqsqlParameter p1 = coll.AddWithValue("p1", DBNull.Value);
			PqsqlParameter p2 = coll.AddWithValue("p2", 123);
			PqsqlParameter p0 = coll.AddWithValue("p0", "text");

			Assert.AreEqual(3, coll.Count);

			Assert.AreEqual(p1.ParameterName, coll[0].ParameterName);
			Assert.AreEqual(p1.DbType, coll[0].DbType);
			Assert.AreEqual(p1.Direction, coll[0].Direction);
			Assert.AreEqual(p1.Value, coll[0].Value);

			Assert.AreEqual(p2.ParameterName, coll[1].ParameterName);
			Assert.AreEqual(p2.DbType, coll[1].DbType);
			Assert.AreEqual(p2.Direction, coll[1].Direction);
			Assert.AreEqual(p2.Value, coll[1].Value);

			Assert.AreEqual(p0.ParameterName, coll[2].ParameterName);
			Assert.AreEqual(p0.DbType, coll[2].DbType);
			Assert.AreEqual(p0.Direction, coll[2].Direction);
			Assert.AreEqual(p0.Value, coll[2].Value);

			Assert.AreEqual(p0.ParameterName, ":p0");
			Assert.AreEqual(p0.DbType, DbType.Object);
			Assert.AreEqual(p0.Direction, ParameterDirection.Input);
			Assert.AreEqual(p0.Value, "text");

			Assert.AreEqual(p1.ParameterName, ":p1");
			Assert.AreEqual(p1.DbType, DbType.Object);
			Assert.AreEqual(p1.Direction, ParameterDirection.Input);
			Assert.AreEqual(p1.Value, DBNull.Value);

			Assert.AreEqual(p2.ParameterName, ":p2");
			Assert.AreEqual(p2.DbType, DbType.Object);
			Assert.AreEqual(p2.Direction, ParameterDirection.Input);
			Assert.AreEqual(p2.Value, 123);

			PqsqlParameter p2_new = coll.AddWithValue("p2", 74.11);

			Assert.AreEqual(p2_new.ParameterName, coll[1].ParameterName);
			Assert.AreEqual(p2_new.DbType, coll[1].DbType);
			Assert.AreEqual(p2_new.Direction, coll[1].Direction);
			Assert.AreEqual(p2_new.Value, coll[1].Value);

			Assert.AreEqual(p2_new.ParameterName, ":p2");
			Assert.AreEqual(p2_new.DbType, DbType.Object);
			Assert.AreEqual(p2_new.Direction, ParameterDirection.Input);
			Assert.AreEqual(p2_new.Value, 74.11);
		}
	}
}
