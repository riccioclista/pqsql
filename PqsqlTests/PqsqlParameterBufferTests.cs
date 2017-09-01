using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlParameterBufferTests
	{
		[TestMethod]
		public void PqsqlParameterBufferTest1()
		{
			PqsqlParameterBuffer buf = new PqsqlParameterBuffer();

			IntPtr ptyps; // oid*
			IntPtr pvals; // char**
			IntPtr plens; // int*
			IntPtr pfrms; // int*

			int num = buf.GetQueryParams(out ptyps, out pvals, out plens, out pfrms);

			Assert.AreEqual(0, num);
			Assert.AreEqual(IntPtr.Zero, ptyps);
			Assert.AreEqual(IntPtr.Zero, pvals);
			Assert.AreEqual(IntPtr.Zero, plens);
			Assert.AreEqual(IntPtr.Zero, pfrms);

			buf.Dispose();
		}

		[TestMethod]
		public void PqsqlParameterBufferTest2()
		{
			PqsqlParameterBuffer buf = new PqsqlParameterBuffer();

			buf.AddParameter(new PqsqlParameter("p1", DbType.Int32));
			buf.AddParameter(new PqsqlParameter("p2", DbType.Boolean) { Direction = ParameterDirection.InputOutput });
			buf.AddParameter(new PqsqlParameter("p3", DbType.Double) { Direction = ParameterDirection.Output });

			IntPtr ptyps; // oid*
			IntPtr pvals; // char**
			IntPtr plens; // int*
			IntPtr pfrms; // int*

			int num = buf.GetQueryParams(out ptyps, out pvals, out plens, out pfrms);

			Assert.AreEqual(2, num);
			Assert.AreNotEqual(IntPtr.Zero, ptyps);
			Assert.AreNotEqual(IntPtr.Zero, pvals);
			Assert.AreNotEqual(IntPtr.Zero, plens);
			Assert.AreNotEqual(IntPtr.Zero, pfrms);

			buf.Dispose();
		}

		[TestMethod]
		public void PqsqlParameterBufferTest3()
		{
			PqsqlParameterBuffer buf = new PqsqlParameterBuffer();

			buf.AddParameter(new PqsqlParameter("p1", DbType.Int32));
			buf.AddParameter(new PqsqlParameter("p2", DbType.Boolean) { Direction = ParameterDirection.InputOutput });
			buf.AddParameter(new PqsqlParameter("p3", DbType.Double) { Direction = ParameterDirection.Output });

			IntPtr ptyps; // oid*
			IntPtr pvals; // char**
			IntPtr plens; // int*
			IntPtr pfrms; // int*

			int num = buf.GetQueryParams(out ptyps, out pvals, out plens, out pfrms);

			Assert.AreEqual(2, num);
			Assert.AreNotEqual(IntPtr.Zero, ptyps);
			Assert.AreNotEqual(IntPtr.Zero, pvals);
			Assert.AreNotEqual(IntPtr.Zero, plens);
			Assert.AreNotEqual(IntPtr.Zero, pfrms);

			// after we have called GetQueryParams above, we cannot add more parameters
			buf.AddParameter(new PqsqlParameter("p4", DbType.Int64));
			num = buf.GetQueryParams(out ptyps, out pvals, out plens, out pfrms);

			Assert.AreEqual(2, num);
			Assert.AreNotEqual(IntPtr.Zero, ptyps);
			Assert.AreNotEqual(IntPtr.Zero, pvals);
			Assert.AreNotEqual(IntPtr.Zero, plens);
			Assert.AreNotEqual(IntPtr.Zero, pfrms);

			// but we can reset the buffer
			buf.Clear();
			num = buf.GetQueryParams(out ptyps, out pvals, out plens, out pfrms);

			Assert.AreEqual(0, num);
			Assert.AreEqual(IntPtr.Zero, ptyps);
			Assert.AreEqual(IntPtr.Zero, pvals);
			Assert.AreEqual(IntPtr.Zero, plens);
			Assert.AreEqual(IntPtr.Zero, pfrms);

			// and start all over again
			buf.AddParameter(new PqsqlParameter("p1", DbType.Int32));
			buf.AddParameter(new PqsqlParameter("p2", DbType.Boolean) { Direction = ParameterDirection.InputOutput });
			buf.AddParameter(new PqsqlParameter("p3", DbType.Double) { Direction = ParameterDirection.Output });
			buf.AddParameter(new PqsqlParameter("p4", DbType.Int64));
			num = buf.GetQueryParams(out ptyps, out pvals, out plens, out pfrms);

			Assert.AreEqual(3, num);
			Assert.AreNotEqual(IntPtr.Zero, ptyps);
			Assert.AreNotEqual(IntPtr.Zero, pvals);
			Assert.AreNotEqual(IntPtr.Zero, plens);
			Assert.AreNotEqual(IntPtr.Zero, pfrms);

			buf.Dispose();
		}

		[TestMethod]
		public void PqsqlParameterBufferTest4()
		{
			PqsqlParameterCollection col = new PqsqlParameterCollection();

			col.AddRange(new PqsqlParameter[]
			{
				new PqsqlParameter { ParameterName = "p0", Value = null },
				new PqsqlParameter { ParameterName = "p1", Value = DBNull.Value },
				new PqsqlParameter("p2", DbType.Int32),
				new PqsqlParameter("p3", DbType.Boolean) { Direction = ParameterDirection.InputOutput },
				new PqsqlParameter("p4", DbType.Double) { Direction = ParameterDirection.Output }
			});

			using (PqsqlParameterBuffer buf = new PqsqlParameterBuffer(col))
			{
				IntPtr ptyps; // oid*
				IntPtr pvals; // char**
				IntPtr plens; // int*
				IntPtr pfrms; // int*

				int num = buf.GetQueryParams(out ptyps, out pvals, out plens, out pfrms);

				Assert.AreEqual(4, num);
				Assert.AreNotEqual(IntPtr.Zero, ptyps);
				Assert.AreNotEqual(IntPtr.Zero, pvals);
				Assert.AreNotEqual(IntPtr.Zero, plens);
				Assert.AreNotEqual(IntPtr.Zero, pfrms);
			}
		}

		[TestMethod]
		public void PqsqlParameterBufferTest5()
		{
			using (PqsqlParameterBuffer buf = new PqsqlParameterBuffer())
			{
				buf.AddParameter(new PqsqlParameter { ParameterName = "p1", Value = 1 });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p2", Value = 2.0 });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p3", Value = "3" });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p4", Value = new byte[] { 0x0, 0x1, 0x2, 0x3 } });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p5", Value = new DateTimeOffset(DateTime.UtcNow) });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p6", Value = new TimeSpan(1,2,3) });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p7", Value = (short)7 });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p8", Value = (long)8 });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p9", Value = true });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p10", Value = DateTime.Now });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p11", Value = 11.111M });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p12", Value = (sbyte)127 });
				buf.AddParameter(new PqsqlParameter { ParameterName = "p13", Value = (float)13.45 });

				IntPtr ptyps; // oid*
				IntPtr pvals; // char**
				IntPtr plens; // int*
				IntPtr pfrms; // int*

				int num = buf.GetQueryParams(out ptyps, out pvals, out plens, out pfrms);

				Assert.AreEqual(13, num);
				Assert.AreNotEqual(IntPtr.Zero, ptyps);
				Assert.AreNotEqual(IntPtr.Zero, pvals);
				Assert.AreNotEqual(IntPtr.Zero, plens);
				Assert.AreNotEqual(IntPtr.Zero, pfrms);
			}
		}
	}
}