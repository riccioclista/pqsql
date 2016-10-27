﻿using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;

namespace PqsqlTests
{
	[TestClass]
	public class PqsqlTypeRegistryTests
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

			DateTime nowlocal0 = now0.ToLocalTime();
			DateTime nowutc0 = now0.ToUniversalTime();

			TimeZoneInfo newtzi = TimeZoneInfo.CreateCustomTimeZone(new_tz, ts, new_tz, new_tz);

			DateTimeOffset off0 = TimeZoneInfo.ConvertTime(now0, newtzi, TimeZoneInfo.Local);

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

	}
}
