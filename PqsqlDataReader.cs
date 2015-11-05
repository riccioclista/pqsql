﻿using System;
using System.Text;
using System.Data.Common;
using System.Data;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;

namespace Pqsql
{
	/// <summary>
	/// cache column name and type information
	/// </summary>
	internal class PqsqlColInfo
	{
		public PqsqlDbType Oid { get; set; }
		public int Format { get; set; }
		public int Modifier { get; set; }
		public int Size { get; set; }
		public string Name { get; set; }
		public string DataTypeName { get; set; }
		public Type Type  { get; set; }
		public Func<IntPtr, int, int, int, object> GetValue { get; set; }
	};


	internal class PqsqlEnumerator : IEnumerator
	{
		protected PqsqlDataReader mReader;

		public PqsqlEnumerator(PqsqlDataReader reader)
		{
			mReader = reader;
		}

		// Summary:
		//     Gets the current element in the collection.
		//
		// Returns:
		//     The current element in the collection.
		//
		// Exceptions:
		//   System.InvalidOperationException:
		//     The enumerator is positioned before the first element of the collection or
		//     after the last element.
		public object Current
		{
			get
			{
				object[] vals = new object[mReader.FieldCount];
				mReader.GetValues(vals);
				return vals;
			}
		}

		// Summary:
		//     Advances the enumerator to the next element of the collection.
		//
		// Returns:
		//     true if the enumerator was successfully advanced to the next element; false
		//     if the enumerator has passed the end of the collection.
		//
		// Exceptions:
		//   System.InvalidOperationException:
		//     The collection was modified after the enumerator was created.
		public bool MoveNext()
		{
			return mReader != null && (mReader.Read() || mReader.NextResult());
		}

		//
		// Summary:
		//     Sets the enumerator to its initial position, which is before the first element
		//     in the collection.
		//
		// Exceptions:
		//   System.InvalidOperationException:
		//     The collection was modified after the enumerator was created.
		public void Reset()
		{
			throw new InvalidOperationException("Cannot reset PqsqlDataReader");
		}
	};


	/// <summary>
	/// 
	/// </summary>
	public class PqsqlDataReader : DbDataReader
	{
		// the current PGresult* buffer
		IntPtr mResult;

		/// <summary>
		/// stores query parameters and execution state:
		/// set PqsqlCommand.State to ConnectionState.Executing / ConnectionState.Fetching / ConnectionState.Closed
		/// </summary>
		readonly PqsqlCommand mCmd;

		// fixed connection
		readonly PqsqlConnection mConn;
		// once mConn is ConnectionState.Open, we set mPGConn
		IntPtr mPGConn;

		readonly CommandBehavior mBehaviour;

		// row information of current result set
		PqsqlColInfo[] mRowInfo;
		// after we execute a statement, we retrieve column information
		bool mPopulateRowInfo;

		// row index (-1: nothing read yet, 0: first row, ...)
		int mRowNum;
		// max rows in current result buffer mResult
		int mMaxRows;

		// statement index (-1: nothing executed yet)
		int mStmtNum;

		readonly int mMaxStmt;
		readonly string[] mStatements;


		#region DbDataReader

		#region ctors and dtors

		// Summary:
		//     Initializes a new instance of the PqsqlDataReader class.
		public PqsqlDataReader(PqsqlCommand command, CommandBehavior behavior, string[] statements)
		{
			mCmd = command;
			mConn = command.Connection;
			mPGConn = mConn.PGConnection;

			mBehaviour = behavior;

			mMaxStmt = 0;
			mStmtNum = -1;

			if (statements != null)
			{
				mMaxStmt = statements.Length;
				mStatements = new string[mMaxStmt];
				Array.Copy(statements, mStatements, mMaxStmt);
			}

			Reset();
		}

		protected void Reset()
		{
			// no data available for the current query, set mMaxRows == mRownum.
			// next call to NextResult() will issue the next query and start to Read() again

			// clear mRowInfo only if CommandBehavior.SchemaOnly is off
			if ((mBehaviour & CommandBehavior.SchemaOnly) == 0)
			{
				mRowInfo = null;
			}

			mMaxRows = -1;
			mRowNum = -1;
		}

		~PqsqlDataReader()
		{
			Dispose(false);
		}

		#endregion

		// Summary:
		//     Gets a value indicating the depth of nesting for the current row.
		//
		// Returns:
		//     The depth of nesting for the current row.
		public override int Depth
		{
			get { return 0; }
		}

		//
		// Summary:
		//     Gets the number of columns in the current row.
		//
		// Returns:
		//     The number of columns in the current row.
		//
		// Exceptions:
		//   System.NotSupportedException:
		//     There is no current connection to an instance of SQL Server.
		public override int FieldCount
		{
			get
			{
				if (mResult == IntPtr.Zero)
					return -1;

				return mRowInfo == null ? -1 : mRowInfo.Length;
			}
		}
		//
		// Summary:
		//     Gets a value that indicates whether this System.Data.Common.DbDataReader
		//     contains one or more rows.
		//
		// Returns:
		//     true if the System.Data.Common.DbDataReader contains one or more rows; otherwise
		//     false.
		public override bool HasRows
		{
			get { return mMaxRows > 0; }
		}

		//
		// Summary:
		//     Gets a value indicating whether the System.Data.Common.DbDataReader is closed.
		//
		// Returns:
		//     true if the System.Data.Common.DbDataReader is closed; otherwise false.
		//
		// Exceptions:
		//   System.InvalidOperationException:
		//     The System.Data.SqlClient.SqlDataReader is closed.
		public override bool IsClosed
		{
			get
			{
				if (mConn == null)
					return true;

				ConnectionState s = mConn.State;

				if (s == ConnectionState.Closed || (s & ConnectionState.Broken) > 0)
					return true;

				return false;
			}
		}
		//
		// Summary:
		//     Gets the number of rows changed, inserted, or deleted by execution of the
		//     SQL statement.
		//
		// Returns:
		//     The number of rows changed, inserted, or deleted. -1 for SELECT statements;
		//     0 if no rows were affected or the statement failed.
		public override int RecordsAffected
		{
			get
			{
				if (mResult == IntPtr.Zero)
					return 0;

				ExecStatus s = (ExecStatus) PqsqlWrapper.PQresultStatus(mResult);

				switch (s)
				{
					case ExecStatus.PGRES_SINGLE_TUPLE: // SELECT
					case ExecStatus.PGRES_TUPLES_OK:
						return -1;

					case ExecStatus.PGRES_COMMAND_OK: // UPDATE / DELETE / INSERT / CREATE * / ...
						unsafe
						{
							sbyte *tuples = PqsqlWrapper.PQcmdTuples(mResult);

							if (tuples == null || *tuples == '\0') // NULL pointer or empty string
								break;
						
							string t = new string(tuples);
							return Convert.ToInt32(t);
						}
				}
				
				return 0;
			}
		}
		//
		// Summary:
		//     Gets the number of fields in the System.Data.Common.DbDataReader that are
		//     not hidden.
		//
		// Returns:
		//     The number of fields that are not hidden.
		//public virtual int VisibleFieldCount
		//{
		//	get;
		//}

		// Summary:
		//     Gets the value of the specified column as an instance of System.Object.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.IndexOutOfRangeException:
		//     The index passed was outside the range of 0 through System.Data.IDataRecord.FieldCount.
		public override object this[int ordinal]
		{
			get { return GetValue(ordinal); }
		}

		//
		// Summary:
		//     Gets the value of the specified column as an instance of System.Object.
		//
		// Parameters:
		//   name:
		//     The name of the column.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.IndexOutOfRangeException:
		//     No column with the specified name was found.
		public override object this[string name]
		{
			get { return this[GetOrdinal(name)]; }
		}

		bool mClosing;

		// Summary:
		//     Closes the System.Data.Common.DbDataReader object.
		public override void Close()
		{
			if (mClosing)
				return;

			mClosing = true;

			if (mConn == null || mConn.State == ConnectionState.Closed)
				return;

			if (mCmd != null)
			{
				mCmd.Cancel(); // cancel currently running command
				Consume();
			}

			if ((mBehaviour & CommandBehavior.CloseConnection) > 0)
			{
				mConn.Close();
			}

			// reset state
			Reset();
		}

		// consume remaining input, see http://www.postgresql.org/docs/9.4/static/libpq-async.html
		protected void Consume()
		{
			if (mResult != IntPtr.Zero)
			{
				// always free mResult
				PqsqlWrapper.PQclear(mResult);
			}

			if (mPGConn == IntPtr.Zero)
				return;

			// consume all remaining results until we reach the NULL result
			while ((mResult = PqsqlWrapper.PQgetResult(mPGConn)) != IntPtr.Zero)
			{
				// always free mResult
				PqsqlWrapper.PQclear(mResult);
			}
		}


		#region Dispose

		//
		// Summary:
		//     Releases all resources used by the current instance of the System.Data.Common.DbDataReader
		//     class.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool mDisposed;

		//
		// Summary:
		//     Releases the managed resources used by the System.Data.Common.DbDataReader
		//     and optionally releases the unmanaged resources.
		//
		// Parameters:
		//   disposing:
		//     true to release managed and unmanaged resources; false to release only unmanaged
		//     resources.
		protected override void Dispose(bool disposing)
		{
			if (mDisposed)
			{
				return;
			}

			if (disposing)
			{
				// always release mConnection (must not throw exception)
				Close();
			}

			// do not release connection this is handled in Close()
			mPGConn = IntPtr.Zero;

			if (mResult != IntPtr.Zero)
			{
				PqsqlWrapper.PQclear(mResult);
				mResult = IntPtr.Zero;
			}

			base.Dispose(disposing);
			mDisposed = true;
		}

		#endregion

		#region datatype and bounds checks

		protected void CheckOrdinal(int ordinal)
		{
			if (mResult == IntPtr.Zero)
				throw new IndexOutOfRangeException("No tuple available");

			if (ordinal < 0 || ordinal >= FieldCount)
				throw new IndexOutOfRangeException("Column " + ordinal + " out of range");
		}

		protected void CheckOrdinalType(int ordinal, PqsqlDbType oid)
		{
			CheckOrdinal(ordinal);

			PqsqlDbType coloid = mRowInfo[ordinal].Oid;
			if (oid != coloid)
				throw new InvalidCastException("Trying to access datatype " + coloid + " as datatype " + oid);
		}

		#endregion

		//
		// Summary:
		//     Gets the value of the specified column as a Boolean.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override bool GetBoolean(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Boolean);
			return GetBoolean(mResult, mRowNum, ordinal);
		}

		internal static bool GetBoolean(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return PqsqlBinaryFormat.pqbf_get_bool(v) > 0;
		}
		//
		// Summary:
		//     Gets the value of the specified column as a byte.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override byte GetByte(int ordinal)
		{
			CheckOrdinal(ordinal); // oid does not matter
			return GetByte(mResult, mRowNum, ordinal);
		}

		internal static byte GetByte(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return PqsqlBinaryFormat.pqbf_get_byte(v);
		}
		//
		// Summary:
		//     Reads a stream of bytes from the specified column, starting at location indicated
		//     by dataOffset, into the buffer, starting at the location indicated by bufferOffset.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		//   dataOffset:
		//     The index within the row from which to begin the read operation.
		//
		//   buffer:
		//     The buffer into which to copy the data.
		//
		//   bufferOffset:
		//     The index with the buffer to which the data will be copied.
		//
		//   length:
		//     The maximum number of characters to read.
		//
		// Returns:
		//     The actual number of bytes read.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Bytea);

			int blen = PqsqlWrapper.PQgetlength(mResult, mRowNum, ordinal);

			// report length of bytea column when buffer is null 
			if (buffer == null)
				return blen;
			
			// check lower bounds
			if (dataOffset < 0 || bufferOffset < 0 || length <= 0)
				return 0;

			int bufferLength = buffer.Length;
			uint maxLength = (uint) (bufferLength - bufferOffset);

			// check upper bounds
			if (dataOffset >= blen || bufferOffset >= bufferLength || length > maxLength)
				return 0;

			IntPtr v = PqsqlWrapper.PQgetvalue(mResult, mRowNum, ordinal);
			ulong n = (ulong) Math.Min(length, blen);

			unsafe
			{
				fixed (byte* b = buffer)
				{
					sbyte* sb = (sbyte*) b + bufferOffset;
					PqsqlBinaryFormat.pqbf_get_bytea(v + (int) dataOffset, sb, n);
				}
			}

			return (long) n;
		}
		//
		// Summary:
		//     Gets the value of the specified column as a single character.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override char GetChar(int ordinal)
		{
			throw new NotImplementedException("GetChar");
		}
		//
		// Summary:
		//     Reads a stream of characters from the specified column, starting at location
		//     indicated by dataIndex, into the buffer, starting at the location indicated
		//     by bufferIndex.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		//   dataOffset:
		//     The index within the row from which to begin the read operation.
		//
		//   buffer:
		//     The buffer into which to copy the data.
		//
		//   bufferOffset:
		//     The index with the buffer to which the data will be copied.
		//
		//   length:
		//     The maximum number of characters to read.
		//
		// Returns:
		//     The actual number of characters read.
		public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException("GetChars");
		}
		//
		// Summary:
		//     Returns a System.Data.Common.DbDataReader object for the requested column
		//     ordinal.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     A System.Data.Common.DbDataReader object.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new PqsqlDataReader GetData(int ordinal)
		{
			throw new NotImplementedException("GetData");
		}
		//
		// Summary:
		//     Gets name of the data type of the specified column.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     A string representing the name of the data type.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override string GetDataTypeName(int ordinal)
		{
			CheckOrdinal(ordinal);
			return mRowInfo[ordinal].DataTypeName;
		}
		//
		// Summary:
		//     Gets the value of the specified column as a System.DateTime object.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override DateTime GetDateTime(int ordinal)
		{
			CheckOrdinal(ordinal);

			PqsqlDbType oid = mRowInfo[ordinal].Oid;
			switch (oid)
			{
				case PqsqlDbType.Timestamp:
				case PqsqlDbType.TimestampTZ:
					return GetDateTime(mResult, mRowNum, ordinal);

				case PqsqlDbType.Time:
				case PqsqlDbType.TimeTZ:
					return GetTime(mResult, mRowNum, ordinal);

				case PqsqlDbType.Date:
					return GetDate(mResult, mRowNum, ordinal);

				default:
					throw new InvalidCastException("Trying to access datatype " + oid + " as datatype DateTime");
			}
		}

		internal static DateTime GetDateTime(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);

			long sec;
			int usec;

			unsafe
			{
				PqsqlBinaryFormat.pqbf_get_timestamp(v, &sec, &usec);
			}

			long ticks = PqsqlBinaryFormat.UnixEpochTicks + sec * TimeSpan.TicksPerSecond + usec * 10;

			DateTime dt = new DateTime(ticks);

			return dt;
		}

		internal static DateTime GetDate(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			int d;

			d = PqsqlBinaryFormat.pqbf_get_date(v);

			long ticks = PqsqlBinaryFormat.UnixEpochTicks + d * TimeSpan.TicksPerSecond;

			DateTime dt = new DateTime(ticks);

			return dt;
		}

		internal static DateTime GetTime(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			long t;

			t = PqsqlBinaryFormat.pqbf_get_time(v);

			long ticks = PqsqlBinaryFormat.UnixEpochTicks + t * TimeSpan.TicksPerSecond;

			DateTime dt = new DateTime(ticks);

			return dt;
		}

		public TimeSpan GetTimeSpan(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Interval);
			return GetInterval(mResult, mRowNum, ordinal);
		}

		internal static TimeSpan GetInterval(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);

			long offset;
			int day;
			int month;

			// from timestamp.h:
			//typedef struct
			//{
			//  int64      time;                   /* all time units other than days, months and years */
			//  int32           day;                    /* days, after time for alignment */
			//  int32           month;                  /* months and years, after time for alignment */
			//} Interval;
			unsafe
			{
				PqsqlBinaryFormat.pqbf_get_interval(v, &offset, &day, &month);
			}

			// TimeSpan is a time period expressed in 100-nanosecond units,
			// whereas interval is in 1-microsecond resolution
			TimeSpan ts = new TimeSpan(offset * 10 + day * TimeSpan.TicksPerDay);

			// from timestamp.h:
			// #define DAYS_PER_YEAR   365.25  /* assumes leap year every four years */
			// #define MONTHS_PER_YEAR 12
			if (month != 0)
			{
				long month_to_days = (long) (month / 12.0 * 365.25);
				ts += TimeSpan.FromTicks(month_to_days * TimeSpan.TicksPerDay);
			}

#if false
			double days = day;

			if (month > 0 || month < 0)
			{
				days += (month / 12) * 365.25;
			}

			if (days > 0 || days < 0)
			{
				ts += TimeSpan.FromDays(days);
			}
#endif

			return ts;
		}

		//
		// Summary:
		//     Returns a System.Data.Common.DbDataReader object for the requested column
		//     ordinal that can be overridden with a provider-specific implementation.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     A System.Data.Common.DbDataReader object.
		//protected virtual DbDataReader GetDbDataReader(int ordinal);
		//
		// Summary:
		//     Gets the value of the specified column as a System.Decimal object.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override decimal GetDecimal(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Numeric);
			return (decimal) GetNumeric(mResult, mRowNum, ordinal, mRowInfo[ordinal].Modifier);
		}

		internal static double GetNumeric(IntPtr res, int row, int ordinal, int typmod)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return PqsqlBinaryFormat.pqbf_get_numeric(v, typmod);
		}

		//
		// Summary:
		//     Gets the value of the specified column as a double-precision floating point
		//     number.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override double GetDouble(int ordinal)
		{
			CheckOrdinal(ordinal);

			PqsqlColInfo ci = mRowInfo[ordinal];

			switch (ci.Oid)
			{
				case PqsqlDbType.Float8:
					return GetDouble(mResult, mRowNum, ordinal);
				case PqsqlDbType.Float4:
					return GetFloat(mResult, mRowNum, ordinal);
				case PqsqlDbType.Numeric:
					return GetNumeric(mResult, mRowNum, ordinal, ci.Modifier);
			}

			throw new InvalidCastException("Trying to access datatype " + ci.Oid + " as datatype Float8");
		}

		internal static double GetDouble(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return PqsqlBinaryFormat.pqbf_get_float8(v);
		}
		//
		// Summary:
		//     Returns an System.Collections.IEnumerator that can be used to iterate through
		//     the rows in the data reader.
		//
		// Returns:
		//     An System.Collections.IEnumerator that can be used to iterate through the
		//     rows in the data reader.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override IEnumerator GetEnumerator()
		{
			return new PqsqlEnumerator(this);
		}
		//
		// Summary:
		//     Gets the data type of the specified column.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The data type of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override Type GetFieldType(int ordinal)
		{
			CheckOrdinal(ordinal);
			return mRowInfo[ordinal].Type;
		}
		//
		// Summary:
		//     Gets the value of the specified column as a single-precision floating point
		//     number.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override float GetFloat(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Float4);
			return GetFloat(mResult, mRowNum, ordinal);
		}

		internal static float GetFloat(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return PqsqlBinaryFormat.pqbf_get_float4(v);
		}
		//
		// Summary:
		//     Gets the value of the specified column as a globally-unique identifier (GUID).
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override Guid GetGuid(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Uuid);
			return GetGuid(mResult, mRowNum, ordinal);
		}

		internal static Guid GetGuid(IntPtr res, int row, int ordinal)
		{
			// TODO IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return new Guid(); // TODO PqsqlBinaryFormat.pqbf_get_uuid(v);
		}
		//
		// Summary:
		//     Gets the value of the specified column as a 16-bit signed integer.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override short GetInt16(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Int2);
			return GetInt16(mResult, mRowNum, ordinal);
		}

		internal static short GetInt16(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return PqsqlBinaryFormat.pqbf_get_int2(v);
		}
		//
		// Summary:
		//     Gets the value of the specified column as a 32-bit signed integer.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override int GetInt32(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Int4);
			return GetInt32(mResult, mRowNum, ordinal);
		}

		internal static int GetInt32(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return PqsqlBinaryFormat.pqbf_get_int4(v);
		}
		//
		// Summary:
		//     Gets the value of the specified column as a 64-bit signed integer.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override long GetInt64(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Int8);
			return GetInt64(mResult, mRowNum, ordinal);
		}

		internal static long GetInt64(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return PqsqlBinaryFormat.pqbf_get_int8(v);
		}
		//
		// Summary:
		//     Gets the name of the column, given the zero-based column ordinal.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The name of the specified column.
		public override string GetName(int ordinal)
		{
			CheckOrdinal(ordinal);
			return mRowInfo[ordinal].Name;
		}
		//
		// Summary:
		//     Gets the column ordinal given the name of the column.
		//
		// Parameters:
		//   name:
		//     The name of the column.
		//
		// Returns:
		//     The zero-based column ordinal.
		//
		// Exceptions:
		//   System.IndexOutOfRangeException:
		//     The name specified is not a valid column name.
		public override int GetOrdinal(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			int col = PqsqlWrapper.PQfnumber(mResult, name);

			if (col == -1)
				throw new IndexOutOfRangeException("No column with name " + name + " was found");

			return col;
		}

		//
		// Summary:
		//     Returns the provider-specific field type of the specified column.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The System.Type object that describes the data type of the specified column.
		//[EditorBrowsable(EditorBrowsableState.Never)]
		//public virtual Type GetProviderSpecificFieldType(int ordinal);
		//
		// Summary:
		//     Gets the value of the specified column as an instance of System.Object.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//[EditorBrowsable(EditorBrowsableState.Never)]
		//public virtual object GetProviderSpecificValue(int ordinal);
		//
		// Summary:
		//     Gets all provider-specific attribute columns in the collection for the current
		//     row.
		//
		// Parameters:
		//   values:
		//     An array of System.Object into which to copy the attribute columns.
		//
		// Returns:
		//     The number of instances of System.Object in the array.
		//[EditorBrowsable(EditorBrowsableState.Never)]
		//public virtual int GetProviderSpecificValues(object[] values);

		//
		// Summary:
		//     Returns a System.Data.DataTable that describes the column metadata of the
		//     System.Data.Common.DbDataReader.
		//
		// Returns:
		//     A System.Data.DataTable that describes the column metadata.
		//
		// Exceptions:
		//   System.InvalidOperationException:
		//     The System.Data.SqlClient.SqlDataReader is closed.
		public override DataTable GetSchemaTable()
		{
			if ((mBehaviour & CommandBehavior.KeyInfo) == 0)
				throw new InvalidOperationException("Cannot call GetSchemaTable without KeyInfo");

			throw new NotImplementedException("GetSchemaTable not implemented");
		}

		//
		// Summary:
		//     Gets the value of the specified column as an instance of System.String.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		//
		// Exceptions:
		//   System.InvalidCastException:
		//     The specified cast is not valid.
		public override string GetString(int ordinal)
		{
			CheckOrdinal(ordinal);

			PqsqlDbType oid = mRowInfo[ordinal].Oid;
			if (oid != PqsqlDbType.Text && oid != PqsqlDbType.Varchar && oid != PqsqlDbType.Unknown)
			{
				throw new InvalidCastException("Trying to access datatype " + oid + " as datatype Text");
			}

			return GetString(mResult, mRowNum, ordinal);	
		}

		internal static string GetString(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);

			IntPtr utp;
			int len;
			unsafe
			{
				utp = PqsqlBinaryFormat.pqbf_get_unicode_text(v, &len);
			}

			if (utp == IntPtr.Zero)
				return null;

			string uni = Marshal.PtrToStringUni(utp, len);
			PqsqlBinaryFormat.pqbf_free_unicode_text(utp);
			return uni;
		}

		//
		// Summary:
		//     Gets the value of the specified column as an instance of System.Object.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     The value of the specified column.
		public override object GetValue(int ordinal)
		{
			CheckOrdinal(ordinal);

			PqsqlColInfo ci = mRowInfo[ordinal];

			if (ci.Oid == PqsqlDbType.Bytea)
			{
				int n = (int) GetBytes(ordinal, 0, null, 0, 0);
				byte[] bs = new byte[n];
				n = (int) GetBytes(ordinal, 0, bs, 0, n);

				if (n != bs.Length)
				{
					throw new IndexOutOfRangeException("Received wrong number of bytes for byte array");
				}

				return bs;
			}

			return ci.GetValue(mResult, mRowNum, ordinal, ci.Modifier);
		}

		//
		// Summary:
		//     Populates an array of objects with the column values of the current row.
		//
		// Parameters:
		//   values:
		//     An array of System.Object into which to copy the attribute columns.
		//
		// Returns:
		//     The number of instances of System.Object in the array.
		public override int GetValues(object[] values)
		{
			if (values == null)
				throw new ArgumentNullException("values");

			int count = Math.Min(FieldCount, values.Length);
			for (int i = 0; i < count; i++)
			{
				values[i] = GetValue(i);
			}
			return count;
		}

		//
		// Summary:
		//     Gets a value that indicates whether the column contains nonexistent or missing
		//     values.
		//
		// Parameters:
		//   ordinal:
		//     The zero-based column ordinal.
		//
		// Returns:
		//     true if the specified column is equivalent to System.DBNull; otherwise false.
		public override bool IsDBNull(int ordinal)
		{
			CheckOrdinal(ordinal);
			return PqsqlWrapper.PQgetisnull(mResult, mRowNum, ordinal) == 1;
		}

		//
		// Summary:
		//     Advances the reader to the next result when reading the results of a batch
		//     of statements.
		//
		// Returns:
		//     true if there are more result sets; otherwise false.
		public override bool NextResult()
		{
			// finished with all query statements
			if (mStmtNum >= mMaxStmt - 1)
				return false;

			mStmtNum++; // set next statement
			mPopulateRowInfo = true; // next Read() will get fresh row information

			if (Execute() == false)
			{
				string err = mConn.GetErrorMessage();
				throw new PqsqlException(err);
			}

			// start with a new result set
			Reset();

			if (!Read())
			{
				// in case an intermediate result set is empty
				// we can call NextResult() again
				return mStmtNum < mMaxStmt - 1;
			}

			return true;
		}


		//
		// Summary:
		//     Advances the reader to the next record in a result set.
		//
		// Returns:
		//     true if there are more rows; otherwise false.
		public override bool Read()
		{
			if (mMaxStmt == 0 || mStmtNum == -1) // no queries available or nothing executed yet
				return false;

			if (!mPopulateRowInfo) // increase row counter to the next row in mResult
			{
				mRowNum++;
			}

			if (mRowNum >= mMaxRows) // mResult is exhausted or never called before
			{
				if (mResult != IntPtr.Zero)
				{
					// free mResult from a former PQgetResult() call and continue fetching a new mResult
					PqsqlWrapper.PQclear(mResult);
				}

				// fetch the next tuple(s)
				mResult = PqsqlWrapper.PQgetResult(mPGConn);

				// rewind mResult indexes
				mRowNum = mRowNum > -1 ? 0 : -1;
				mMaxRows = -1;
			}

			if (mResult != IntPtr.Zero) // result buffer not exhausted
			{
				ExecStatus s = (ExecStatus) PqsqlWrapper.PQresultStatus(mResult);

				if (s == ExecStatus.PGRES_COMMAND_OK)
				{
					// nothing to do, we just executed a command without result
					Consume(); // consume remaining results, PqsqlDatareader.RecordsAffected will return 0
					Reset();
					return false;
				}

				if (s != ExecStatus.PGRES_SINGLE_TUPLE && s != ExecStatus.PGRES_TUPLES_OK)
				{
					Consume(); // consume remaining results

					string err = mConn.GetErrorMessage();
					throw new PqsqlException(err);
				}

				if (mMaxRows == -1) // get number of tuples in a fresh result buffer
				{
					mMaxRows = PqsqlWrapper.PQntuples(mResult);
				}

				if (mPopulateRowInfo) // first row of current statement => just get column information
				{
					mPopulateRowInfo = false; // done populating row information

					PopulateRowInfo();

					if ((mBehaviour & CommandBehavior.SchemaOnly) > 0)
					{
						// we keep mRowInfo here since CommandBehavior.SchemaOnly is on
						Reset();
					}

					// we retrieved the schema, next call to Read() will advance mRowNum and we can call GetXXX()
					return mMaxRows > 0;
				}

				if (mRowNum < mMaxRows)
					return true;

				// fetch the last result to clean up internal libpq state
				PqsqlWrapper.PQclear(mResult);
				mResult = PqsqlWrapper.PQgetResult(mPGConn);
			}

			// result buffer exhausted, this was the last result of the current query
			Reset();
			return false;
		}

		// setup mRowInfo for current statement mStatements[mStmtNum]
		private void PopulateRowInfo()
		{
			int n = PqsqlWrapper.PQnfields(mResult); // get number of columns
			mRowInfo = new PqsqlColInfo[n];

			for (int o = 0; o < n; o++)
			{
				mRowInfo[o] = new PqsqlColInfo();

				PqsqlDbType oid = (PqsqlDbType) PqsqlWrapper.PQftype(mResult, o); // column type
				mRowInfo[o].Oid = oid;

				unsafe
				{
					sbyte* name = PqsqlWrapper.PQfname(mResult, o); // column name
					mRowInfo[o].Name = new string(name); // TODO UTF-8 encoding ignored here!
				}

				mRowInfo[o].Size = PqsqlWrapper.PQfsize(mResult, o); // column datatype size
				mRowInfo[o].Modifier = PqsqlWrapper.PQfmod(mResult, o); // column modifier (e.g., varchar(n))
				mRowInfo[o].Format = PqsqlWrapper.PQfformat(mResult, o); // data format (1: binary, 0: text)

				PqsqlTypeNames.PqsqlTypeName tn = PqsqlTypeNames.Get(oid); // lookup OID
				mRowInfo[o].DataTypeName = tn.Name; // cache PG datatype name
				mRowInfo[o].Type = tn.Type; // cache corresponding Type
				mRowInfo[o].GetValue = tn.GetValue; // cache GetValue function
			}
		}

		/// <summary>
		/// executes the next statement
		/// </summary>
		/// <returns></returns>
		protected bool Execute()
		{
			// convert query string to utf8
			byte[] utf8query = Encoding.UTF8.GetBytes(mStatements[mStmtNum]);
			
			if (utf8query.Length == 0)
				return false;

			unsafe
			{
				int num_param = 0;
				IntPtr ptyps = IntPtr.Zero; // oid*
				IntPtr pvals = IntPtr.Zero; // char**
				IntPtr plens = IntPtr.Zero; // int*
				IntPtr pfrms = IntPtr.Zero; // int*

				PqsqlParameterCollection pc = mCmd.Parameters;

				if (pc != null)
				{
					IntPtr pb = pc.PGParameters; // pqparam_buffer*
					num_param = PqsqlBinaryFormat.pqpb_get_num(pb);
					ptyps = PqsqlBinaryFormat.pqpb_get_types(pb);
					pvals = PqsqlBinaryFormat.pqpb_get_vals(pb);
					plens = PqsqlBinaryFormat.pqpb_get_lens(pb);
					pfrms = PqsqlBinaryFormat.pqpb_get_frms(pb);
				}

				fixed (byte* pq = utf8query)
				{
					if (PqsqlWrapper.PQsendQueryParams(mPGConn, pq, num_param, ptyps, pvals, plens, pfrms, 1) == 0)
						return false;
				}
				
				if (PqsqlWrapper.PQsetSingleRowMode(mPGConn) == 0)
					return false;
			}

			return true;
		}

		#endregion
	}
}
