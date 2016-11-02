using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlDataAdapterTests
	{
		private PqsqlConnection mConnection;

		private PqsqlCommand mCmd;

		#region Additional test attributes

		[TestInitialize]
		public void MyTestInitialize()
		{
			mConnection = new PqsqlConnection("host=localhost; port=5432; user=postgres; dbname=postgres; connect_timeout=3");
			mCmd = mConnection.CreateCommand();
		}

		[TestCleanup]
		public void MyTestCleanup()
		{
			mCmd.Dispose();
			mConnection.Dispose();
		}

		#endregion

		[TestMethod]
		public void PqsqlDataAdapterTest1()
		{
			// get current_database() name
			mCmd.CommandText = "current_database";
			mCmd.CommandType = CommandType.StoredProcedure;
			object curdb = mCmd.ExecuteScalar();

			// fetch pg_database tuple
			mCmd.CommandText = "select datname,pg_encoding_to_char(encoding),datcollate,datctype,datallowconn from pg_database where datname=:dat";
			mCmd.CommandType = CommandType.Text;

			PqsqlParameter datpar = mCmd.CreateParameter();
			datpar.ParameterName = "dat";
			datpar.DbType = DbType.String;
			datpar.Value = curdb;
			mCmd.Parameters.Add(datpar);
			
			DataSet ds = new DataSet();
			using (PqsqlDataAdapter adapter = new PqsqlDataAdapter(mCmd))
			{
				adapter.Fill(ds);
			}

			Assert.AreEqual(1, ds.Tables.Count, "wrong table count");

			int tables = 0;
			int rows = 0;
			int columns = 0;

			foreach (DataTable table in ds.Tables)
			{
				foreach (DataRow row in table.Rows)
				{
					foreach (object item in row.ItemArray)
					{
						// read item
						switch (columns)
						{
						case 0: // datname
							Assert.AreEqual("postgres", item, "wrong database name");
							break;
						case 1: // encoding
							Assert.AreEqual("UTF8", item, "wrong encoding id");
							break;
						case 2: // datcollate
						case 3: // datctype
							Assert.IsNotNull(item);
							break;
						case 4: // datallowconn
							Assert.AreEqual(true, item, "we must be allowed to connect");
							break;
						}
						columns++;
					}
					rows++;
				}
				tables++;
			}

			Assert.AreEqual(1, tables, "wrong table count");
			Assert.AreEqual(1, rows, "wrong row count");
			Assert.AreEqual(5, columns, "wrong column count");
		}
	}
}
