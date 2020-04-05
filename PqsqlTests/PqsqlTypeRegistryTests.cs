using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlTypeRegistryTests
	{
		private static string connectionString = string.Empty;

		private PqsqlConnection mConnection;

		private PqsqlCommand mCmd;

		#region Additional test attributes

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			connectionString = context.Properties["connectionString"].ToString();
		}

		[TestInitialize]
		public void TestInitialize()
		{
			mConnection = new PqsqlConnection(connectionString);
			mCmd = mConnection.CreateCommand();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			mCmd.Dispose();
			mConnection.Dispose();
		}

		#endregion

		[TestMethod]
		public void PqsqlTypeRegistryTest1()
		{
			PqsqlTransaction tran = mConnection.BeginTransaction();
			string old_tz = mConnection.TimeZone;

			mCmd.CommandText = "set timezone to 'Asia/Hong_Kong';" +
													"select localtimestamp, now(), now() at time zone 'Europe/Vienna', '2016-10-01 12:00:00' at time zone 'UTC', '2016-10-01 12:00:00' at time zone 'Europe/Vienna', utc_offset from pg_timezone_names where name = current_setting('TIMEZONE');";
			mCmd.CommandType = CommandType.Text;

			PqsqlDataReader reader = mCmd.ExecuteReader();

			bool read = reader.Read();
			Assert.IsFalse(read);

			string new_tz = mConnection.TimeZone;

			Assert.AreNotEqual(old_tz, new_tz);

			bool hasnext = reader.NextResult();
			Assert.IsTrue(hasnext);

			read = reader.Read();
			Assert.IsTrue(read);

			DateTime now0 = reader.GetDateTime(0);
			DateTime now1 = reader.GetDateTime(1);
			DateTime now2 = reader.GetDateTime(2);
			DateTime now3 = reader.GetDateTime(3);
			DateTime now4 = reader.GetDateTime(4);
			TimeSpan ts = reader.GetTimeSpan(5);
			reader.Close();

			DateTime nowutc0 = now0.ToUniversalTime();

			TimeZoneInfo tzi_from_china_pgsql = TimeZoneInfo.CreateCustomTimeZone(new_tz, ts, new_tz, new_tz);

#if WIN32
			var tz = "China Standard Time";
#else
			var tz = "Asia/Shanghai";
#endif
			TimeZoneInfo tzi_from_china_sys = TimeZoneInfo.FindSystemTimeZoneById(tz);

			TimeSpan china_off = tzi_from_china_sys.GetUtcOffset(nowutc0);
			TimeSpan local_off = TimeZoneInfo.Local.GetUtcOffset(nowutc0);
			DateTimeOffset nowlocal0 = (nowutc0 + local_off - china_off).ToLocalTime();

			DateTimeOffset dto_from_pgsql_to_sys = TimeZoneInfo.ConvertTime(now0, tzi_from_china_pgsql, tzi_from_china_sys);
			DateTimeOffset dto_from_pgsql_to_localtime = TimeZoneInfo.ConvertTime(now0, tzi_from_china_pgsql, TimeZoneInfo.Local);

			Assert.AreEqual(nowutc0, dto_from_pgsql_to_sys);
			Assert.AreEqual(nowlocal0, dto_from_pgsql_to_localtime);

			tran.Rollback();
		}


		[TestMethod]
		public void PqsqlTypeRegistryTest2()
		{
			mCmd.CommandText = "select localtimestamp, now()::timestamp, now(), now() at time zone 'UTC', '1999-01-01 00:00:00'::timestamp at time zone 'UTC', '2000-07-01 00:00:00'::timestamp at time zone 'Europe/Vienna'";
			mCmd.CommandType = CommandType.Text;

			PqsqlDataReader reader = mCmd.ExecuteReader();

			bool read = reader.Read();
			Assert.IsTrue(read);

			DateTime localtimestamp0 = reader.GetDateTime(0);
			DateTimeOffset localtimestampoff0 = reader.GetDateTimeOffset(0);

			DateTime nownotz1 = reader.GetDateTime(1);
			DateTimeOffset nownotzoff1 = reader.GetDateTimeOffset(1);

			DateTime nowtz2 = reader.GetDateTime(2);
			DateTimeOffset nowtzoff2 = reader.GetDateTimeOffset(2);

			DateTime nowutc3 = reader.GetDateTime(3);
			DateTimeOffset nowutcoff3 = reader.GetDateTimeOffset(3);

			DateTime ts19990101000000_4 = reader.GetDateTime(4);
			DateTimeOffset ts19990101000000_off4 = reader.GetDateTimeOffset(4);

			DateTime ts20000701000000_5 = reader.GetDateTime(5);
			DateTimeOffset ts20000701000000_off5 = reader.GetDateTimeOffset(5);

			DateTimeOffset off0 = TimeZoneInfo.ConvertTime(new DateTimeOffset(localtimestamp0.Ticks, TimeSpan.Zero), TimeZoneInfo.Local);
			DateTimeOffset off1 = TimeZoneInfo.ConvertTime(new DateTimeOffset(nownotz1.Ticks, TimeSpan.Zero), TimeZoneInfo.Local);
			DateTimeOffset off2 = TimeZoneInfo.ConvertTime(new DateTimeOffset(nowtz2.Ticks, TimeSpan.Zero), TimeZoneInfo.Local);
			DateTimeOffset off3 = TimeZoneInfo.ConvertTime(new DateTimeOffset(nowutc3.Ticks, TimeSpan.Zero), TimeZoneInfo.Local);
			DateTimeOffset off4 = TimeZoneInfo.ConvertTime(new DateTimeOffset(ts19990101000000_4.Ticks, TimeSpan.Zero), TimeZoneInfo.Local);
			DateTimeOffset off5 = TimeZoneInfo.ConvertTime(new DateTimeOffset(ts20000701000000_5.Ticks, TimeSpan.Zero), TimeZoneInfo.Local);

			Assert.AreEqual(localtimestampoff0, off0);
			Assert.AreEqual(nownotzoff1, off1);
			Assert.AreEqual(nowtzoff2, off2);
			Assert.AreEqual(nowutcoff3, off3);
			Assert.AreEqual(ts19990101000000_off4, off4);
			Assert.AreEqual(ts20000701000000_off5, off5);

			read = reader.Read();
			Assert.IsFalse(read);
		}

		[TestMethod]
		public void PqsqlTypeRegistryTest3()
		{
			mCmd.CommandText = "select 'YES'::information_schema.yes_or_no;";
			mCmd.CommandType = CommandType.Text;

			PqsqlDataReader reader = mCmd.ExecuteReader();

			bool read = reader.Read();
			Assert.IsTrue(read);

			string yes = reader.GetString(0);

			Assert.AreEqual("YES", yes);

			read = reader.Read();
			Assert.IsFalse(read);
		}

		[TestMethod]
		public void PqsqlTypeRegistryTest4()
		{
			mCmd.CommandText = "select 'YES'::citext";
			mCmd.CommandType = CommandType.Text;

			using (PqsqlCommand check = new PqsqlCommand("select oid from pg_extension where extname='citext'", mConnection))
			using (PqsqlCommand create = new PqsqlCommand("create extension citext", mConnection))
			using (PqsqlCommand drop = new PqsqlCommand("drop extension if exists citext", mConnection))
			{
				object o = null;

				try
				{
					o = check.ExecuteScalar();

					if (o == null)
					{
						int aff = create.ExecuteNonQuery();
						Assert.AreEqual(0, aff);
					}

					PqsqlDataReader reader = mCmd.ExecuteReader();

					bool read = reader.Read();
					Assert.IsTrue(read);

					object yes = reader.GetValue(0); // must access by GetValue, GetString verifies typoid
					Assert.AreEqual("YES", yes);

					reader.Close();
				}
				finally
				{
					if (o == null)
					{
						int aff = drop.ExecuteNonQuery();
						Assert.AreEqual(0, aff);
					}
				}
			}
		}

		[TestMethod]
		public void PqsqlTypeRegistryTest5()
		{
			mCmd.CommandText = "select :ts0,:ts1,:ts2,:ts3,:ts4";
			mCmd.CommandType = CommandType.Text;

			PqsqlParameter p0 = new PqsqlParameter("ts0", DbType.DateTimeOffset, new DateTimeOffset(2016, 1, 1, 0, 0, 0, 0, TimeSpan.Zero));
			mCmd.Parameters.Add(p0);

			PqsqlParameter p1 = new PqsqlParameter
			{
				ParameterName = "ts1",
				PqsqlDbType = PqsqlDbType.Unknown, // let PqsqlParameterCollection guess the type
				Value = new DateTimeOffset(new DateTime(2016, 1, 1, 0, 0, 0, 0, DateTimeKind.Local))
			};
			mCmd.Parameters.Add(p1);

			PqsqlParameter p2 = new PqsqlParameter("ts2", DbType.DateTime, new DateTime(2016, 1, 1, 0, 0, 0, 0, DateTimeKind.Local));
			mCmd.Parameters.Add(p2);

			PqsqlParameter p3 = new PqsqlParameter("ts3", DbType.DateTime, new DateTime(2016, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
			mCmd.Parameters.Add(p3);

			PqsqlParameter p4 = new PqsqlParameter("ts4", DbType.DateTime, new DateTime(2016, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
			mCmd.Parameters.Add(p4);

			PqsqlDataReader reader = mCmd.ExecuteReader();

			bool read = reader.Read();
			Assert.IsTrue(read);

			DateTime ts0 = reader.GetDateTime(0);
			DateTimeOffset tstz0 = reader.GetDateTimeOffset(0);
			DateTimeOffset tsutc0 = tstz0.ToUniversalTime();
			DateTimeOffset tslocal0 = tstz0.ToLocalTime();

			Assert.AreEqual(ts0, tsutc0.DateTime);

			DateTime ts1 = reader.GetDateTime(1);
			DateTimeOffset tstz1 = reader.GetDateTimeOffset(1);
			DateTimeOffset tsutc1 = tstz1.ToUniversalTime();
			DateTimeOffset tslocal1 = tstz1.ToLocalTime();

			DateTime ts2 = reader.GetDateTime(2);
			DateTimeOffset tstz2 = reader.GetDateTimeOffset(2);
			DateTimeOffset tsutc2 = tstz2.ToUniversalTime();
			DateTimeOffset tslocal2 = tstz2.ToLocalTime();

			Assert.AreEqual(ts2, tsutc2.UtcDateTime);
			Assert.AreEqual(ts2.ToLocalTime(), tstz2.LocalDateTime);
			Assert.AreEqual(ts2.ToLocalTime(), tsutc2.LocalDateTime);

			DateTime ts3 = reader.GetDateTime(3);
			DateTimeOffset tstz3 = reader.GetDateTimeOffset(3);
			DateTimeOffset tsutc3 = tstz3.ToUniversalTime();
			DateTimeOffset tslocal3 = tstz3.ToLocalTime();

			Assert.AreEqual(ts3, tstz3.UtcDateTime);
			Assert.AreEqual(ts3, tsutc3.DateTime);

			DateTime ts4 = reader.GetDateTime(4);
			DateTimeOffset tstz4 = reader.GetDateTimeOffset(4);
			DateTimeOffset tsutc4 = tstz4.ToUniversalTime();
			DateTimeOffset tslocal4 = tstz4.ToLocalTime();

			Assert.AreEqual(ts4, tstz4.UtcDateTime);
			Assert.AreEqual(ts4, tsutc4.DateTime);

			read = reader.Read();
			Assert.IsFalse(read);
		}

		[TestMethod]
		public void PqsqlTypeRegistryTest6()
		{
			Action[] actions = new Action[20];

			// stress test user-defined type setup
			for (int i = 0; i < 20; i++)
			{
				actions[i] = () =>
				{
					using (PqsqlConnection conn = new PqsqlConnection(mConnection.ConnectionString))
					using (PqsqlCommand cmd = new PqsqlCommand("select 'hello world'::citext", conn))
					using (PqsqlDataReader reader = cmd.ExecuteReader())
					{
						bool read = reader.Read();
						Assert.IsTrue(read);
						object helloWorld = reader.GetValue(0); // must access by GetValue, GetString verifies typoid
						Assert.AreEqual("hello world", helloWorld);
						read = reader.Read();
						Assert.IsFalse(read);
					}
				};
			}

			using (PqsqlCommand check = new PqsqlCommand("select oid from pg_extension where extname='citext'", mConnection))
			using (PqsqlCommand create = new PqsqlCommand("create extension citext", mConnection))
			using (PqsqlTransaction t = mConnection.BeginTransaction())
			{
				object o = null;

				try
				{
					check.Transaction = t;
					o = check.ExecuteScalar();

					if (o == null)
					{
						create.Transaction = t;
						int aff = create.ExecuteNonQuery();
						Assert.AreEqual(0, aff);
					}

					t.Commit();

					Parallel.Invoke(actions);
				}
				finally
				{
					if (o == null)
					{
						using (PqsqlCommand drop = new PqsqlCommand("drop extension if exists citext", mConnection))
						{
							int aff = drop.ExecuteNonQuery();
							Assert.AreEqual(0, aff);
						}
					}
				}
			}
		}
	}
}
