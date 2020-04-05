using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Pqsql
{
	internal static partial class UnsafeNativeMethods
	{
		//
		// Routines for formatting and parsing frontend/backend binary messages
		//
		internal static unsafe class PqsqlBinaryFormat
		{
			#region timestamp and interval constants

			// unix timestamp 0 in ticks: DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;
			private const long UnixEpochTicks = 621355968000000000;

			// TimeSpan is a time period expressed in 100-nanosecond units,
			// whereas interval is in 1-microsecond resolution
			private const int UsecFactor = 10;

			// timestamp.h constants

			// DAYS_PER_YEAR
			private const double DaysPerYear = 365.25;  /* assumes leap year every four years */

			// MONTHS_PER_YEAR
			private const int MonthsPerYear = 12;

			#endregion

			#region timestamp / date and DateTime conversions

			public static void GetTimestamp(DateTime dt, out long sec, out int usec)
			{
				if (dt == DateTime.MaxValue) // timestamp 'infinity'
				{
					sec = Int64.MaxValue;
					usec = 0;
				}
				else if (dt == DateTime.MinValue) // timestamp '-infinity'
				{
					sec = Int64.MinValue;
					usec = 0;
				}
				else
				{
					// we always interpret dt as Utc timestamp and ignore DateTime.Kind value
					long ticks = dt.Ticks - UnixEpochTicks;
					sec = ticks / TimeSpan.TicksPerSecond;
					usec = (int) (ticks % TimeSpan.TicksPerSecond / UsecFactor);
				}
			}

			public static long GetTicksFromTimestamp(long sec, int usec)
			{
				switch (sec)
				{
				case Int64.MinValue: // timestamp '-infinity'
					return DateTime.MinValue.Ticks;
				case Int64.MaxValue: // timestamp 'infinity'
					return DateTime.MaxValue.Ticks;
				default:
					return UnixEpochTicks + sec * TimeSpan.TicksPerSecond + usec * UsecFactor;
				}
			}

			public static DateTime GetDateTimeFromDate(int year, int month, int day)
			{
				if (year == Int32.MaxValue && month == Int32.MaxValue && day == Int32.MaxValue)
				{
					return DateTime.MaxValue;
				}

				if (year == Int32.MinValue && month == Int32.MinValue && day == Int32.MinValue)
				{
					return DateTime.MinValue;
				}

				if (year < 1 || year > 9999)
				{
					throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, "Year {0} out of range", year));
				}

				if (month < 1 || month > 12)
				{
					throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, "Month {0} out of range", month));
				}

				if (day < 1 || day > 31)
				{
					throw new ArgumentOutOfRangeException(string.Format(CultureInfo.InvariantCulture, "Day {0} out of range", day));
				}

				return new DateTime(year, month, day);
			}

			public static void GetDate(DateTime dt, out int year, out int month, out int day)
			{
				if (dt == DateTime.MaxValue) // date 'infinity'
				{
					year = Int32.MaxValue;
					month = Int32.MaxValue;
					day = Int32.MaxValue;
				}
				else if (dt == DateTime.MinValue) // date '-infinity'
				{
					year = Int32.MinValue;
					month = Int32.MinValue;
					day = Int32.MinValue;
				}
				else
				{
					year = dt.Year;
					month = dt.Month;
					day = dt.Day;
				}
			}

			#endregion

			#region interval / time / timetz and TimeSpan conversions

			public static void GetInterval(TimeSpan ts, out long offset, out int day, out int month)
			{
				int total_days = ts.Days;

				offset = (ts.Ticks - total_days * TimeSpan.TicksPerDay) / UsecFactor;
				month = (int)(MonthsPerYear * total_days / DaysPerYear);
				day = total_days - (int)(month * DaysPerYear / MonthsPerYear);
			}

			public static TimeSpan GetTimeSpan(long offset, int day, int month)
			{
				// from timestamp.h:
				//typedef struct
				//{
				//  int64      time;                   /* all time units other than days, months and years */
				//  int32           day;               /* days, after time for alignment */
				//  int32           month;             /* months and years, after time for alignment */
				//} Interval;
				TimeSpan ts = new TimeSpan(offset * UsecFactor + day * TimeSpan.TicksPerDay);

				if (month != 0)
				{
					long month_to_days = (long)(month / (double)MonthsPerYear * DaysPerYear);
					ts += TimeSpan.FromTicks(month_to_days * TimeSpan.TicksPerDay);
				}

				return ts;
			}

			public static long GetTicksFromTime(int hour, int min, int sec, int fsec)
			{
				return hour * TimeSpan.TicksPerHour +
					min * TimeSpan.TicksPerMinute +
					sec * TimeSpan.TicksPerSecond +
					fsec * UsecFactor;
			}

			public static void GetTime(TimeSpan ts, out int hour, out int min, out int sec, out int fsec)
			{
				hour = ts.Hours;
				min = ts.Minutes;
				sec = ts.Seconds;
				fsec = (int)(ts.Ticks - (hour * TimeSpan.TicksPerHour) - (min * TimeSpan.TicksPerMinute) - (sec * TimeSpan.TicksPerSecond));
				fsec = fsec / UsecFactor;
			}

			public static long GetTicksFromTimeTZ(int hour, int min, int sec, int fsec, int tz)
			{
				// calculate offset relative to localtime
				int offsetSeconds = (int) TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds - tz;

				return hour * TimeSpan.TicksPerHour +
					min * TimeSpan.TicksPerMinute +
					sec * TimeSpan.TicksPerSecond +
					fsec * UsecFactor +
					offsetSeconds * TimeSpan.TicksPerSecond;
			}

			public static void GetTimeTZ(TimeSpan ts, out int hour, out int min, out int sec, out int fsec, out int tz)
			{
				hour = ts.Hours;
				min = ts.Minutes;
				sec = ts.Seconds;
				fsec = (int)(ts.Ticks - (hour * TimeSpan.TicksPerHour) - (min * TimeSpan.TicksPerMinute) - (sec * TimeSpan.TicksPerSecond));
				fsec = fsec / UsecFactor;
				tz = (int) TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds; // interpret ts as localtime
			}

			#endregion

			#region interface to pqparse statement parser

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqparse_init(IntPtr variables);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqparse_destroy(IntPtr pstate);

			[DllImport("libpqbinfmt.dll")]
			public static extern uint pqparse_num_statements(IntPtr pstate);

			[DllImport("libpqbinfmt.dll")]
			public static extern int pqparse_num_unknown_variables(IntPtr pstate);

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqparse_get_statements(IntPtr pstate);

			[DllImport("libpqbinfmt.dll")]
			public static extern int pqparse_add_statements(IntPtr pstate, byte[] buffer);

			#endregion

			#region PQExpBuffer

			[DllImport("libpqbinfmt.dll")]
			public static extern long pqbf_get_buflen(IntPtr s);

			[DllImport("libpqbinfmt.dll")]
			public static extern sbyte* pqbf_get_bufval(IntPtr s);

			#endregion

			#region interface to pqparam_buffer

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqpb_create();

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqpb_free(IntPtr pb);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqpb_reset(IntPtr pb);

			[DllImport("libpqbinfmt.dll")]
			public static extern int pqpb_get_num(IntPtr pb);

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqpb_get_types(IntPtr pb);

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqpb_get_vals(IntPtr pb);

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqpb_get_lens(IntPtr pb);

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqpb_get_frms(IntPtr pb);

			[DllImport("libpqbinfmt.dll")]
			public static extern uint pqpb_get_type(IntPtr pb, int i);

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqpb_get_val(IntPtr pb, int i);

			[DllImport("libpqbinfmt.dll")]
			public static extern int pqpb_get_len(IntPtr pb, int i);

			#endregion

			#region encode datatype as binary parameter

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_null(IntPtr pb, uint oid);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_text(IntPtr pb, sbyte* t, uint oid);

#if WIN32
			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_unicode_text(IntPtr pb, char* t, uint oid);
#endif

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_bool(IntPtr pb, int b);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_bytea(IntPtr pb, sbyte* buf, ulong len);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_char(IntPtr pb, sbyte c);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_int8(IntPtr pb, long i);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_int4(IntPtr pb, int i);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_oid(IntPtr pb, uint i);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_int2(IntPtr pb, short i);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_float4(IntPtr pb, float f);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_float8(IntPtr pb, double d);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_numeric(IntPtr pb, double d);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_interval(IntPtr pbb, long offset, int day, int month);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_timestamp(IntPtr pbb, long sec, int usec, uint oid);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_date(IntPtr p, int year, int month, int day);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_time(IntPtr p, int hour, int min, int sec, int fsec);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_timetz(IntPtr p, int hour, int min, int sec, int fsec, int tz);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_add_array(IntPtr pbb, IntPtr a, uint oid);

			#endregion

			#region encode datatype to binary PQExpBuffer

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_text(IntPtr s, sbyte* t);

#if WIN32
			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_unicode_text(IntPtr s, char* t);
#endif

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_bool(IntPtr s, int b);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_bytea(IntPtr s, sbyte* buf, ulong len);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_char(IntPtr s, sbyte c);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_int8(IntPtr s, long i);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_int4(IntPtr s, int i);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_oid(IntPtr s, uint i);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_int2(IntPtr s, short i);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_float4(IntPtr s, float f);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_float8(IntPtr s, double d);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_numeric(IntPtr s, double d);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_interval(IntPtr s, long offset, int day, int month);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_timestamp(IntPtr s, long sec, int usec);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_date(IntPtr p, int year, int month, int day);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_time(IntPtr p, int hour, int min, int sec, int fsec);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_timetz(IntPtr p, int hour, int min, int sec, int fsec, int tz);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_array(IntPtr s, int ndim, int flags, uint oid, int[] dim, int[] lbound);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_set_array_itemlength(IntPtr a, int itemlen);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_update_array_itemlength(IntPtr a, long offset, int itemlen);

			#endregion

			#region decode datatype from binary message

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqbf_get_text(IntPtr p, ulong* len);

#if WIN32
			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqbf_get_unicode_text(IntPtr p, int* len);
#endif

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_free_unicode_text(IntPtr ptr);

			[DllImport("libpqbinfmt.dll")]
			public static extern byte pqbf_get_byte(IntPtr p);

			[DllImport("libpqbinfmt.dll")]
			public static extern int pqbf_get_bool(IntPtr p);

			[DllImport("libpqbinfmt.dll")]
			public static extern sbyte pqbf_get_char(IntPtr p);

			[DllImport("libpqbinfmt.dll")]
			public static extern long pqbf_get_int8(IntPtr p);

			[DllImport("libpqbinfmt.dll")]
			public static extern int pqbf_get_int4(IntPtr p);

			[DllImport("libpqbinfmt.dll")]
			public static extern uint pqbf_get_oid(IntPtr p);

			[DllImport("libpqbinfmt.dll")]
			public static extern short pqbf_get_int2(IntPtr p);

			[DllImport("libpqbinfmt.dll")]
			public static extern float pqbf_get_float4(IntPtr p);

			[DllImport("libpqbinfmt.dll")]
			public static extern double pqbf_get_float8(IntPtr p);

			[DllImport("libpqbinfmt.dll")]
			public static extern double pqbf_get_numeric(IntPtr p, int typmod);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_get_bytea(IntPtr p, sbyte* buf, ulong len);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_get_interval(IntPtr p, long* offset, int* day, int* month);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_get_timestamp(IntPtr p, long* sec, int* usec);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_get_date(IntPtr p, int *year, int *month, int *day);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_get_time(IntPtr p, int *hour, int *min, int *sec, int *fsec);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqbf_get_timetz(IntPtr p, int* hour, int* min, int* sec, int* fsec, int* tz);

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqbf_get_array(IntPtr p, int* ndim, int* flags, uint* oid, ref IntPtr dim, ref IntPtr lbound);

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqbf_get_array_value(IntPtr p, int* itemlen);

			#endregion

			#region interface to pqcopy_buffer

			[DllImport("libpqbinfmt.dll")]
			public static extern IntPtr pqcb_create(IntPtr conn, int num_cols);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqcb_free(IntPtr pc);

			[DllImport("libpqbinfmt.dll")]
			public static extern void pqcb_reset(IntPtr pc, int num_cols);

			[DllImport("libpqbinfmt.dll")]
			public static extern int pqcb_put_col(IntPtr pc, sbyte* val, uint len);

			[DllImport("libpqbinfmt.dll")]
			public static extern int pqcb_put_end(IntPtr pc);

			#endregion
		}

	}
}
