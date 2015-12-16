using System;
using System.Data.Common;
using System.Data;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

// declare SchemaTable column information
//
// Item1: column position
// Item2: default value
// Item3: isKey
// Item4: notNull
// Item5: isUnique
// Item6: isUpdateable
using SchemaTableColumnInfo = System.Tuple<short, object, bool, bool, bool, bool>;

namespace Pqsql
{
	/// <summary>
	/// cache column name and type information
	/// </summary>
	internal sealed class PqsqlColInfo
	{
		public PqsqlDbType Oid { get; set; }
		public int Modifier { get; set; }
		public int Size { get; set; }
		public string ColumnName { get; set; }
		public string DataTypeName { get; set; }
		public Type ProviderType  { get; set; }
		public Func<IntPtr, int, int, int, object> GetValue { get; set; }
	};


	/// <summary>
	/// Specific row names used in PqsqlDataReader.GetSchemaTable()
	/// </summary>
	internal struct PqsqlSchemaTableColumn
	{
		internal const string TypeOid = "TypeOid";
	};


	/// <summary>
	/// 
	/// </summary>
	public sealed class PqsqlDataReader : DbDataReader
	{
#region PqsqlDataReader statements
			// two queries: retrieve first catalog and table information and then column information
			// parameter :o is table oid
			const string CatalogColumnByTableOid = "SELECT current_catalog, n.nspname, c.relname FROM pg_namespace n, pg_class c WHERE c.relnamespace=n.oid AND c.relkind IN ('r','v') AND c.oid=:o;SELECT ca.attname, ca.attnotnull, ca.attnum, ad.adsrc, pg_column_is_updatable(c.oid, ca.attnum, false), ind.indisunique, ind.indisprimary FROM ((pg_attribute ca LEFT JOIN pg_attrdef ad ON (attrelid = adrelid AND attnum = adnum)) JOIN (pg_class c JOIN pg_namespace nc ON (c.relnamespace = nc.oid)) ON (ca.attrelid = c.oid)) LEFT OUTER JOIN (SELECT a.attname, i.indisunique, i.indisprimary, i.indexrelid, i.indrelid FROM pg_catalog.pg_class ct, pg_catalog.pg_class ci, pg_catalog.pg_attribute a, pg_catalog.pg_index i WHERE ct.oid=i.indrelid AND ci.oid=i.indexrelid AND a.attrelid=ci.oid AND ct.oid=:o) ind ON (ind.attname = ca.attname) WHERE ca.attnum > 0 and c.oid=:o";
#endregion


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
		// for GetSchemaTable()
		DataTable mSchemaTable;

		// the oid of the referenced table
		private uint mTableOid;
		// after statement execution, we populate column information mRowInfo and fill output parameter mCmd.Parameters
		bool mPopulateAndFill;
		// number of columns
		int mColumns;

		// row index (-1: nothing read yet, 0: first row, ...)
		int mRowNum;
		// max rows in current result buffer mResult
		int mMaxRows;

		// stores the number of records touched after INSERT / UPDATE / DELETE / etc.
		int mRecordsAffected;

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

		private void Reset()
		{
			// no data available for the current query, set mMaxRows == mRownum.
			// next call to NextResult() will issue the next query and start to Read() again

			// clear mRowInfo only if CommandBehavior.SchemaOnly is off
			if ((mBehaviour & CommandBehavior.SchemaOnly) == 0)
			{
				mTableOid = 0; // InvalidOid
				if (mSchemaTable != null)
				{
					mSchemaTable.Dispose();
					mSchemaTable = null;
				}
				mRowInfo = null;
				mColumns = 0;
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
			get { return mRecordsAffected; }
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

			Consume(); // consume remaining results

			if ((mBehaviour & CommandBehavior.CloseConnection) > 0)
			{
				mConn.Close();
			}

			// reset state
			Reset();
		}

		// consume remaining input, see http://www.postgresql.org/docs/9.4/static/libpq-async.html
		internal void Consume()
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

		private void CheckOrdinal(int ordinal)
		{
			if (mResult == IntPtr.Zero)
				throw new IndexOutOfRangeException("No tuple available");

			if (ordinal < 0 || ordinal >= mColumns)
				throw new IndexOutOfRangeException("Column " + ordinal + " out of range");
		}

		private void CheckOrdinalType(int ordinal, PqsqlDbType oid)
		{
			CheckOrdinal(ordinal);

			PqsqlDbType coloid = mRowInfo[ordinal].Oid;
			if (oid != coloid)
				throw new InvalidCastException("Trying to access datatype " + coloid + " as datatype " + oid);
		}

		#endregion



		internal static void GetArray(IntPtr res, int row, int ordinal, out int ndim, out int flags, out PqsqlDbType oid, out int[] dim, out int[] lbound, out IntPtr val)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);

			const int maxdim = 6;
			int size = 0;

			size = Marshal.SizeOf(size) * maxdim;
			IntPtr dimbuf = Marshal.AllocCoTaskMem(size);
			IntPtr lboundbuf = Marshal.AllocCoTaskMem(size);

			unsafe
			{
				fixed (int* d = &ndim)
				{
					fixed (int* f = &flags)
					{
						uint o = 0;
						val = PqsqlBinaryFormat.pqbf_get_array(v, d, f, &o, ref dimbuf, ref lboundbuf);
						oid = (PqsqlDbType) o;
					}
				}
			}

			dim = new int[ndim];
			lbound = new int[ndim];

			Marshal.Copy(dimbuf, dim, 0, ndim);
			Marshal.Copy(lboundbuf, lbound, 0, ndim);

			Marshal.FreeCoTaskMem(dimbuf);
			Marshal.FreeCoTaskMem(lboundbuf);
		}


		internal delegate object GetArrayItem(IntPtr v, int itemlen);

		internal static void FillArray(ref Array a, IntPtr val, int ndim, GetArrayItem gpv)
		{
			if (ndim != 1)
				throw new NotImplementedException("Arrays with ndim=" + ndim + " not supported yet");

			int[] idx = new int[ndim];
			for (int d = 0; d < ndim; d++)
				idx[d] = a.GetLowerBound(d);

			int last = ndim - 1;

			// we only support 1 dimensional arrays for now
			int ub = a.GetUpperBound(last);
			while (idx[last] <= ub)
			{
				int itemlen;
				unsafe
				{
					val = PqsqlBinaryFormat.pqbf_get_array_value(val, &itemlen);
				}

				if (itemlen < 0)
				{
					// nothing to do
					a.SetValue(null, idx);
				}
				else
				{
					object o = gpv(val, itemlen); // in situations where itemlen is not fixed by datatype (text values do not have \0)
					a.SetValue(o, idx);
					val += itemlen;
				}

				idx[last]++;
			}
		}


		internal static Array GetArrayFill(IntPtr res, int row, int ordinal, PqsqlDbType typoid, Type nullable, Type nonNullable, Func<IntPtr, int, object> itemDelegate)
		{
			int ndim;
			int flags;
			PqsqlDbType oid;
			IntPtr val;
			int[] dim;
			int[] lbound;

			GetArray(res, row, ordinal, out ndim, out flags, out oid, out dim, out lbound, out val);

			if (oid != typoid)
			{
				throw new InvalidCastException("Array has wrong datatype " + oid);
			}

			Array a = Array.CreateInstance(flags > 0 ? nullable : nonNullable, dim, lbound);

			FillArray(ref a, val, ndim, (x, len) => itemDelegate(x, len));

			return a;
		}

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

			return GetBytes(mResult, mRowNum, ordinal, dataOffset, buffer, bufferOffset, length);
		}

		internal static long GetBytes(IntPtr res, int row, int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
		{
			int blen = PqsqlWrapper.PQgetlength(res, row, ordinal);

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

			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
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

		// TODO double loses precision, should we get the string representation of the numeric here?
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
			return new DbEnumerator(this);
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
			return mRowInfo[ordinal].ProviderType;
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

		public uint GetOid(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Oid);
			return GetOid(mResult, mRowNum, ordinal);
		}

		internal static uint GetOid(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return PqsqlBinaryFormat.pqbf_get_oid(v);
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
			return mRowInfo[ordinal].ColumnName;
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
			if (mPopulateAndFill)
				throw new InvalidOperationException("PqsqlDataReader did not populate schema information yet");

			if (mSchemaTable != null)
				return mSchemaTable;

			// populates mSchemaTable
			return PopulateSchemaTable();
		}

		// retrieve schema information from mRowInfo to populate schema DataTable
		private DataTable PopulateSchemaTable()
		{
			mSchemaTable = new DataTable("SchemaTable");

			DataColumnCollection coll = mSchemaTable.Columns;

			// populate columns in this order

			coll.Add(PqsqlSchemaTableColumn.TypeOid, typeof(PqsqlDbType)); // type oid used in PqsqlCommandBuilder.ApplyParameterInfo
			coll.Add(SchemaTableColumn.AllowDBNull, typeof(bool)); // whether value DBNull is allowed.
			coll.Add(SchemaTableColumn.BaseColumnName, typeof(string)); // name of the column in the schema table.
			coll.Add(SchemaTableOptionalColumn.BaseCatalogName, typeof(string)); // name of the catalog associated with the results of the latest query.
			coll.Add(SchemaTableColumn.BaseSchemaName, typeof(string)); // name of the schema in the schema table.
			coll.Add(SchemaTableColumn.BaseTableName, typeof(string)); // name of the table in the schema table.

			coll.Add(SchemaTableColumn.ColumnName, typeof(string)); // name of the column in the schema table.
			coll.Add(SchemaTableColumn.ColumnOrdinal, typeof(int)); // ordinal of the column.
			coll.Add(SchemaTableColumn.ColumnSize, typeof(int)); // size of the column.

			coll.Add(SchemaTableColumn.NumericPrecision, typeof(int)); // precision of the column data, if the data is numeric.
			coll.Add(SchemaTableColumn.NumericScale, typeof(int)); // scale of the column data, if the data is numeric.

			coll.Add(SchemaTableColumn.ProviderType, typeof(string)); // provider-specific data type of the column.
			coll.Add(SchemaTableColumn.DataType, typeof(Type)); // type of data in the column.
			
			// only set key information when the user asked for it
			if ((mBehaviour & CommandBehavior.KeyInfo) == CommandBehavior.KeyInfo)
			{
				coll.Add(SchemaTableColumn.IsKey, typeof(bool)); // whether this column is a key for the table.
				coll.Add(SchemaTableColumn.IsUnique, typeof(bool)); // whether a unique constraint applies to this column.
				coll.Add(SchemaTableColumn.IsAliased, typeof(bool)); // whether this column is aliased.
				coll.Add(SchemaTableColumn.IsExpression, typeof(bool)); // whether this column is an expression.
				coll.Add(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool)); // whether the column values in the column are automatically incremented.
				coll.Add(SchemaTableOptionalColumn.IsRowVersion, typeof(bool)); // whether this column contains row version information.
				coll.Add(SchemaTableOptionalColumn.IsHidden, typeof(bool)); // whether this column is hidden.
				coll.Add(SchemaTableColumn.IsLong, typeof(bool)); // whether this column contains long data.
				coll.Add(SchemaTableOptionalColumn.IsReadOnly, typeof(bool)); // whether this column is read-only.
				coll.Add(SchemaTableOptionalColumn.DefaultValue, typeof(object)); // default value for the column.
			}
		
#if false
		SchemaTableColumn.NonVersionedProviderType // non-versioned provider-specific data type of the column.
		SchemaTableOptionalColumn.ProviderSpecificDataType // provider-specific data type of the column.
		SchemaTableOptionalColumn.AutoIncrementSeed; //     Specifies the value at which the series for new identity columns is assigned.		
		SchemaTableOptionalColumn.AutoIncrementStep; //     Specifies the increment between values in the identity column.
		SchemaTableOptionalColumn.BaseColumnNamespace; //     The namespace of the column.
		SchemaTableOptionalColumn.BaseServerName; //     The server name of the column.
		SchemaTableOptionalColumn.BaseTableNamespace; //     The namespace for the table that contains the column.
		SchemaTableOptionalColumn.ColumnMapping; //     Specifies the mapping for the column.
		SchemaTableOptionalColumn.Expression; //     The expression used to compute the column.
#endif

			FillSchemaTableColumns();

			return mSchemaTable;
		}


		// 
		private void FillSchemaTableColumns()
		{
			string catalogName = string.Empty;
			string schemaName = string.Empty;
			string tableName = string.Empty;

			// column dictionary: maps columnname to (1=colPos, 2=defVal, 3=isKey, 4=notNull, 5=isUnique, 6=isUpdateable)
			Dictionary<string, SchemaTableColumnInfo> colDic = new Dictionary<string, SchemaTableColumnInfo>();

			GetSchemaTableInfo(ref catalogName, ref schemaName, ref tableName, ref colDic);

			DataRowCollection srows = mSchemaTable.Rows;

			for (int i = 0; i < mColumns; i++)
			{
				PqsqlColInfo ci = mRowInfo[i];

				string name = ci.ColumnName;
				int modifier = ci.Modifier;
				int size = ci.Size;

				DataRow row = mSchemaTable.NewRow();
				int j = 0; // set fields by index

				// 1=colPos, 2=defVal, 3=isKey, 4=notNull, 5=isUnique, 6=isUpdateable
				SchemaTableColumnInfo colInfo;

				if (!colDic.TryGetValue(name, out colInfo))
				{
					colInfo = new SchemaTableColumnInfo(-1, null, false, false, false, false);
				}

				row.SetField(j++, ci.Oid); // TypeOid
				row.SetField(j++, !colInfo.Item4); // AllowDBNull
				row.SetField(j++, name); // BaseColumnName
				row.SetField(j++, catalogName); // BaseCatalogName
				row.SetField(j++, schemaName); // BaseSchemaName
				row.SetField(j++, tableName); // BaseTableName

				row.SetField(j++, name); // ColumnName
				row.SetField(j++, i + 1); // ColumnOrdinal

				switch (ci.Oid)
				{
				case PqsqlDbType.Name:
				case PqsqlDbType.Text:
				case PqsqlDbType.Varchar:
				case PqsqlDbType.Unknown:
				case PqsqlDbType.Refcursor:
				case PqsqlDbType.BPChar:
				case PqsqlDbType.Char:
					row.SetField(j++, modifier > -1 ? modifier - 4 : size); // ColumnSize
					row.SetField(j++, 0); // NumericPrecision
					row.SetField(j++, 0); // NumericScale
					break;

				case PqsqlDbType.Numeric:
					row.SetField(j++, size); // ColumnSize
					modifier -= 4;
					row.SetField(j++, (modifier >> 16) & ushort.MaxValue); // NumericPrecision
					row.SetField(j++, modifier & ushort.MaxValue); // NumericScale
					break;

				default:
					row.SetField(j++, size); // ColumnSize
					row.SetField(j++, 0); // NumericPrecision
					row.SetField(j++, 0); // NumericScale
					break;
				}

				row.SetField(j++, ci.DataTypeName); // ProviderType
				row.SetField(j++, ci.ProviderType); // DataType

				if ((mBehaviour & CommandBehavior.KeyInfo) == CommandBehavior.KeyInfo)
				{
					row.SetField(j++, colInfo.Item3); // IsKey
					row.SetField(j++, colInfo.Item5); // IsUnique
					row.SetField(j++, false); // IsAliased TODO
					row.SetField(j++, false); // IsExpression
					row.SetField(j++, false); // IsAutoIncrement TODO
					row.SetField(j++, false); // IsRowVersion
					row.SetField(j++, false); // IsHidden
					row.SetField(j++, false); // IsLong TODO blob?
					row.SetField(j++, !colInfo.Item6); // IsReadOnly
					row.SetField(j, colInfo.Item2); // DefaultValue
				}

				srows.Add(row);
			}
		}

		// create fresh connection to retrieve table and column/key information
		private void GetSchemaTableInfo(ref string catalogName, ref string schemaName, ref string tableName, ref Dictionary<string, Tuple<short, object, bool, bool, bool, bool>> colDic)
		{
			// create fresh connection, we are already active in an executing connection
			using (PqsqlConnection schemaconn = new PqsqlConnection(mConn.ConnectionString))
			using (PqsqlCommand c = new PqsqlCommand(CatalogColumnByTableOid, schemaconn))
			{
				PqsqlParameter parTableOid = new PqsqlParameter
				{
					ParameterName = "o",
					PqsqlDbType = PqsqlDbType.Oid,
					Value = mTableOid
				};

				c.Parameters.Add(parTableOid);

				using (PqsqlDataReader dr = c.ExecuteReader())
				{
					// get catalog and table information
					while (dr.Read())
					{
						catalogName = dr.GetString(0);
						schemaName = dr.GetString(1);
						tableName = dr.GetString(2);
					}

					// issue next query
					dr.NextResult();

					// get column information
					while (dr.Read())
					{
						string colName = dr.GetString(0);
						bool notNull = dr.GetBoolean(1);
						short colPos = dr.GetInt16(2);
						object defVal = dr.GetValue(3);
						bool isUpdateable = dr.GetBoolean(4);
						bool isUnique = dr.GetBoolean(5);
						bool isKey = dr.GetBoolean(6);

						// add column info to dictionary
						colDic.Add(colName, new SchemaTableColumnInfo(colPos, defVal, isKey, notNull, isUnique, isUpdateable));
					}
				}
			}
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

			switch (oid)
			{
				case PqsqlDbType.Text:
				case PqsqlDbType.Varchar:
				case PqsqlDbType.Unknown:
				case PqsqlDbType.Name:
				case PqsqlDbType.Refcursor:
					return GetString(mResult, mRowNum, ordinal);
			}

			throw new InvalidCastException("Trying to access datatype " + oid + " as datatype Text");	
		}

		internal static string GetStringValue(IntPtr v, int itemlen)
		{
			if (itemlen == 0)
			{
				// TODO pqbf_get_unicode_text sometimes fails to retrieve proper strlen(v) when itemlen==0
				return PqsqlUTF8Statement.CreateStringFromUTF8(v);
			}

			IntPtr utp;

			unsafe
			{
				utp = PqsqlBinaryFormat.pqbf_get_unicode_text(v, &itemlen);
			}

			if (utp == IntPtr.Zero)
				return null;

			string uni;
			if (itemlen > 0)
			{
				uni = Marshal.PtrToStringUni(utp, itemlen);
			}
			else
			{
				uni = string.Empty;
			}
			PqsqlBinaryFormat.pqbf_free_unicode_text(utp);
			return uni;
		}

		internal static string GetString(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return GetStringValue(v, 0); // 0...unknown strlen
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

			int count = Math.Min(mColumns, values.Length);
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
			mPopulateAndFill = true; // next Read() below will get fresh row information

			if (Execute() == false)
			{
				string err = mConn.GetErrorMessage();
				throw new PqsqlException("Could not execute statement «" + mStatements[mStmtNum] + "»: " + err);
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

			if (!mPopulateAndFill) // increase row counter to the next row in mResult
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

				//
				// INSERT / UPDATE / DELETE / CREATE statement processing
				//

				if (s == ExecStatus.PGRES_COMMAND_OK)
				{
					mRecordsAffected = GetCmdTuples(s);

					// nothing to do, we just executed a command without result rows
					Consume(); // consume remaining results
					Reset();
					return false;
				}

				//
				// error handling
				//

				if (s != ExecStatus.PGRES_SINGLE_TUPLE && s != ExecStatus.PGRES_TUPLES_OK)
				{
					string err = mConn.GetErrorMessage();
					PqsqlException ex = new PqsqlException(err, mResult);

					Consume(); // consume remaining results

					throw ex;
				}

				//
				// SELECT / FETCH statement processing
				//

				if (mMaxRows == -1) // get number of tuples in a fresh result buffer
				{
					mMaxRows = PqsqlWrapper.PQntuples(mResult);
				}

				// first row of current statement => get column information and fill output parameters
				if (mPopulateAndFill)
				{
					mPopulateAndFill = false; // done populating row information

					mRecordsAffected = -1; // reset for SELECT
					PopulateRowInfoAndOutputParameters();

					if ((mBehaviour & CommandBehavior.SchemaOnly) > 0)
					{
						// we keep mRowInfo here since CommandBehavior.SchemaOnly is on
						Reset();
					}

					// we retrieved the schema and the output parameters
					// next call to Read() will advance mRowNum and we can call GetXXX()
					return mMaxRows > 0;
				}

				// mResult not completely processed?
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

		#endregion

		#region query metadata retrieval

		/// <summary>
		/// retrieve the number of touched rows
		/// </summary>
		/// <returns>number of touched records for UPDATE / DELETE / INSERT / CREATE * / ... statements, otherwise -1</returns>
		private int GetCmdTuples(ExecStatus s)
		{
			switch (s)
			{
				case ExecStatus.PGRES_SINGLE_TUPLE: // SELECT
				case ExecStatus.PGRES_TUPLES_OK:
					return -1;

				case ExecStatus.PGRES_COMMAND_OK: // UPDATE / DELETE / INSERT / CREATE * / ...
					unsafe
					{
						if (mResult == IntPtr.Zero)
							break;

						sbyte* tuples = PqsqlWrapper.PQcmdTuples(mResult);

						if (tuples == null || *tuples == 0x0) // NULL pointer or empty string
							break;

						string t = new string(tuples);
						return Convert.ToInt32(t);
					}
			}
			return 0;
		}

		/// <summary>
		/// setup mRowInfo for current statement mStatements[mStmtNum] and fill OUT and INOUT
		/// parameters from mCmd.Parameters with result tuple of the first row
		/// </summary>
		private void PopulateRowInfoAndOutputParameters()
		{
			mColumns = PqsqlWrapper.PQnfields(mResult); // get number of columns
			mRowInfo = new PqsqlColInfo[mColumns];

			// only set output parameters when we had executed a stored procedure returning at least one row
			bool populateOutputParameters = mMaxRows > 0 && mCmd.CommandType == CommandType.StoredProcedure;
			PqsqlParameterCollection parms = null;

			if (populateOutputParameters)
			{
				parms = mCmd.Parameters;
				if (parms == null || parms.Count <= 0)
				{
					populateOutputParameters = false;
				}
			}

			for (int o = 0; o < mColumns; o++)
			{
				if (mTableOid == 0) // try to get table oid until we find a column that is simple reference to a table column
					mTableOid = PqsqlWrapper.PQftable(mResult, o); // try to get table oid for column o 

				mRowInfo[o] = new PqsqlColInfo();

				PqsqlDbType oid = (PqsqlDbType) PqsqlWrapper.PQftype(mResult, o); // column type
				mRowInfo[o].Oid = oid;

				string colName;
				unsafe
				{
					sbyte* name = PqsqlWrapper.PQfname(mResult, o); // column name
					colName = new string(name); // TODO UTF-8 encoding ignored here!
				}

				int size = PqsqlWrapper.PQfsize(mResult, o); // column datatype size
				int modifier = PqsqlWrapper.PQfmod(mResult, o); // column modifier (e.g., varchar(n))

				mRowInfo[o].ColumnName = colName; // column name
				mRowInfo[o].Size = size; // column size
				mRowInfo[o].Modifier = modifier; // column modifier

				// try to lookup OID, otherwise try to guess type and fetch type specs from DB
				PqsqlTypeRegistry.PqsqlTypeName tn = PqsqlTypeRegistry.Get(oid) ?? PqsqlTypeRegistry.FetchType(oid, mConn.ConnectionString);

				mRowInfo[o].DataTypeName = tn.DataTypeName; // cache PG datatype name
				mRowInfo[o].ProviderType = tn.ProviderType; // cache corresponding ProviderType
				mRowInfo[o].GetValue = tn.GetValue; // cache GetValue function

				if (populateOutputParameters) // use first row to fill corresponding output parameter
				{
					int j = parms.IndexOf(colName);

					if (j < 0)
					{
						// throw error if we didn't find the corresponding parameter name in our parameter list
						PqsqlException e = new PqsqlException("Received unrecognized output parameter «" + colName + "» when calling function «" + mCmd.CommandText + "»");
						e.Hint = "Please adjust parameter names in PqsqlCommand.Parameters";
						throw e;
					}

					// set new Value for found parameter based on 1st row
					// (this even works if parameter direction was wrong)
					parms[j].Value = tn.GetValue(mResult, 0, o, modifier);
				}
			}
		}

		/// <summary>
		/// executes the next statement with PQsendQueryParams
		/// </summary>
		/// <returns>true if and only if current statement was successfully executed</returns>
		private bool Execute()
		{
			string stmt = mStatements[mStmtNum]; // current statement
			CommandBehavior behave = mBehaviour; // result fetching behaviour

			// libpq does not want PQsetSingleRowMode with cursors, just turn it off
			if (stmt.StartsWith("fetch ", true, CultureInfo.InvariantCulture))
			{
				behave &= ~CommandBehavior.SingleRow;
			}

			// convert query string to utf8
			byte[] utf8query = PqsqlUTF8Statement.CreateUTF8Statement(mStatements[mStmtNum]);

			if (utf8query == null || utf8query[0] == 0x0) // null or empty string
				return false;

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

			unsafe
			{
				fixed (byte* pq = utf8query)
				{
					if (PqsqlWrapper.PQsendQueryParams(mPGConn, pq, num_param, ptyps, pvals, plens, pfrms, 1) == 0)
						return false;
				}
			}

			// only set single row mode for non-FETCH statements
			if ((behave & CommandBehavior.SingleRow) > 0)
			{
				if (PqsqlWrapper.PQsetSingleRowMode(mPGConn) == 0)
					return false;
			}

			return true;
		}

		#endregion
	}
}
