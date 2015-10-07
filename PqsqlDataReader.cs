using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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
	};


	public class PqsqlDataReader : DbDataReader
	{
		/// <summary>
		/// the current PGresult* buffer
		/// </summary>
		IntPtr mResult;

		/// <summary>
		/// stores db connection and query parameters.
		/// used to set PqsqlCommand.State to ConnectionState.Executing or ConnectionState.Fetching
		/// </summary>
		readonly PqsqlCommand mCmd;

		readonly CommandBehavior mBehaviour;

		// row information of current result set
		PqsqlColInfo[] mRowInfo;

		// row index (-1: nothing read yet, 0: first row, ...)
		int mRowNum;
		int mMaxRows;

		// statement index (-1: nothing executed yet)
		int mStmtNum;

		readonly int mMaxStmt;
		string[] mStatements;


		#region DbDataReader

		#region ctors and dtors

		// Summary:
		//     Initializes a new instance of the PqsqlDataReader class.
		public PqsqlDataReader(PqsqlCommand command, CommandBehavior behavior, string[] statements)
		{
			mCmd = command;
			mBehaviour = behavior;

			mMaxStmt = statements.Length;
			mStmtNum = -1;
			mStatements = new string[mMaxStmt];
			Array.Copy(statements, mStatements, mMaxStmt);

			Init(ConnectionState.Closed); // set state to 0, neutral value for PqsqlConnection.State
		}

		protected void Init(ConnectionState state)
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
			mCmd.State = state;
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
			get
			{
				return 0;
			}
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
					throw new NotSupportedException("No result received yet");

				return mRowInfo.Length;
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
			get
			{
				return mMaxRows > 0;
			}
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
				ConnectionState s = mCmd.Connection.State;

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
					throw new InvalidOperationException("PqsqlDataReader unpopulated");

				ExecStatus s = (ExecStatus) PqsqlWrapper.PQresultStatus(mResult);

				switch (s)
				{
					case ExecStatus.PGRES_SINGLE_TUPLE: // SELECT
					case ExecStatus.PGRES_TUPLES_OK:
						return -1;

					case ExecStatus.PGRES_COMMAND_OK: // UPDATE / DELETE / INSERT
						string tup = PqsqlWrapper.PQcmdTuples(mResult);
						if (!string.IsNullOrEmpty(tup))
						{
							return Convert.ToInt32(tup);
						}
						break;
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
			get
			{
				return GetValue(ordinal);
			}
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
			get
			{
				return this[GetOrdinal(name)];
			}
		}

		// Summary:
		//     Closes the System.Data.Common.DbDataReader object.
		public override void Close()
		{
			// cancel current command
			mCmd.Cancel();

			// now consume all remaining input, see http://www.postgresql.org/docs/9.4/static/libpq-async.html
			Consume();

			if ((mBehaviour & CommandBehavior.CloseConnection) > 0)
			{
				mCmd.Connection.Close();
			}

			// reset state
			Init(ConnectionState.Closed);
		}
		
		protected void Consume()
		{
			if (mResult != IntPtr.Zero)
			{
				// always free mResult
				PqsqlWrapper.PQclear(mResult);
			}

			while ((mResult = PqsqlWrapper.PQgetResult(mCmd.Connection.PGConnection)) != IntPtr.Zero)
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

		bool mDisposed = false;

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

			// always release mConnection (must not throw exception)
			Close();

			if (disposing)
			{
				mRowInfo = null;
				mStatements = null;
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
				throw new IndexOutOfRangeException("Column " + ordinal.ToString() + " out of range");
		}

		protected void CheckOrdinalType(int ordinal, PqsqlDbType oid)
		{
			if (mResult == IntPtr.Zero)
				throw new IndexOutOfRangeException("No tuple available");

			if (ordinal < 0 || ordinal >= FieldCount)
				throw new IndexOutOfRangeException("Column " + ordinal.ToString() + " out of range");

			if (oid != mRowInfo[ordinal].Oid)
				throw new InvalidCastException("Wrong datatype", (int)oid);
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

			// check lower bounds
			if (dataOffset < 0 || bufferOffset < 0 || length <= 0)
				return 0;

			int blen = PqsqlWrapper.PQgetlength(mResult, mRowNum, ordinal);

			// report length of bytea column when buffer is null 
			if (buffer == null)
				return blen;

			int bufferLength = buffer.Length;
			int maxLength = bufferLength - bufferOffset;

			// check upper bounds
			if (dataOffset >= blen || bufferOffset >= bufferLength || length > maxLength)
				return 0;

			IntPtr v = PqsqlWrapper.PQgetvalue(mResult, mRowNum, ordinal);
			int i;     // offset in bytea column
			int j = 0; // offset in buffer, counter
			
			unsafe
			{
				byte* b = PqsqlBinaryFormat.pqbf_get_bytea(v);

				if (b != null)
				{
					for (i = (int)dataOffset, j = bufferOffset; i < blen && j < maxLength; i++, j++)
					{
						buffer[j] = b[i];
					}
				}
			}

			return j;
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
		public abstract char GetChar(int ordinal);
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
		public abstract long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length);
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
		public DbDataReader GetData(int ordinal);
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
			// TODO check oid
			return GetDateTime(mResult, mRowNum, ordinal);
		}

		internal static DateTime GetDateTime(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return DateTime.Now; // TODO PqsqlBinaryFormat.pqbf_get_bool(v) > 0;
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
		protected virtual DbDataReader GetDbDataReader(int ordinal);
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
			return GetDecimal(mResult, mRowNum, ordinal);
		}

		internal static decimal GetDecimal(IntPtr res, int row, int ordinal)
		{
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
			return (decimal) PqsqlBinaryFormat.pqbf_get_numeric(v);
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
			CheckOrdinalType(ordinal, PqsqlDbType.Float8);
			return GetDouble(mResult, mRowNum, ordinal);
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
		public abstract IEnumerator GetEnumerator();
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
			IntPtr v = PqsqlWrapper.PQgetvalue(res, row, ordinal);
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
		public abstract DataTable GetSchemaTable();
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
				throw new InvalidCastException("Wrong datatype", (int) oid);
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

			if (mRowInfo[ordinal].Oid == PqsqlDbType.Bytea)
			{
				int n = (int) GetBytes(ordinal, 0, null, 0, 0);
				byte[] bs = new byte[n];
				n = (int) GetBytes(ordinal, 0, bs, 0, n);
				return bs;
			}

			return PqsqlTypeNames.GetValue(mRowInfo[ordinal].Oid)(mResult, mRowNum, ordinal);
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
			if (mStmtNum >= mMaxStmt) // finished with all query statements
			{
				return false;
			}

			mStmtNum++; // set next statement

			if (Execute() == false)
			{
				string err = PqsqlWrapper.PQerrorMessage(mCmd.Connection.PGConnection);
				throw new PqsqlException(err);
			}

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
			if (mResult == IntPtr.Zero && mStmtNum == -1)
			{
				// issue the first query
				return NextResult();
			}

			if (mResult != IntPtr.Zero && mRowNum >= mMaxRows)
			{
				// mResult from a former PQgetResult() call is exhausted now,
				// free mResult and continue getting the next mResult
				PqsqlWrapper.PQclear(mResult);
			}

			if (mRowNum >= mMaxRows)
			{
				// fetch the next tuple(s)
				mResult = PqsqlWrapper.PQgetResult(mCmd.Connection.PGConnection);
				mCmd.State = ConnectionState.Fetching;
			}

			if (mResult != IntPtr.Zero)
			{
				mRowNum++; // increase row counter

				if (mRowNum == 0) // first row => get column information
				{
					int n = PqsqlWrapper.PQnfields(mResult); // get number of columns
					mRowInfo = new PqsqlColInfo[n];

					for (int o = 0; o < n; o++)
					{
						PqsqlDbType oid = (PqsqlDbType) PqsqlWrapper.PQftype(mResult, o); // column type
						mRowInfo[o].Oid = oid;
						mRowInfo[o].Name = PqsqlWrapper.PQfname(mResult, o);     // column name
						mRowInfo[o].Size = PqsqlWrapper.PQfsize(mResult, o);     // column datatype size
						mRowInfo[o].Modifier = PqsqlWrapper.PQfmod(mResult, o);  // column modifier (e.g., varchar(n))
						mRowInfo[o].Format = PqsqlWrapper.PQfformat(mResult, o); // data format (1: binary, 0: text)
						mRowInfo[o].DataTypeName = PqsqlTypeNames.GetName(oid);
						mRowInfo[o].Type = PqsqlTypeNames.GetType(oid);
					}

					if ((mBehaviour & CommandBehavior.SchemaOnly) > 0)
					{
						// we keep mRowInfo here since CommandBehavior.SchemaOnly is on
						Init(ConnectionState.Closed);
						return true; // we retrieved the schema
					}

					mMaxRows = PqsqlWrapper.PQntuples(mResult); // get number of tuples
				}
			}
			else
			{
				Init(ConnectionState.Closed);
			}

			return mRowNum < mMaxRows;
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
				IntPtr pc = mCmd.Connection.PGConnection;
				IntPtr pb = mCmd.Parameters.PGParameters;

				int num_param = PqsqlBinaryFormat.pqpb_get_num(pb);
				IntPtr ptyps = PqsqlBinaryFormat.pqpb_get_types(pb);
				IntPtr pvals = PqsqlBinaryFormat.pqpb_get_vals(pb);
				IntPtr plens = PqsqlBinaryFormat.pqpb_get_lens(pb);
				IntPtr pfrms = PqsqlBinaryFormat.pqpb_get_frms(pb);

				fixed (byte* pq = utf8query)
				{
					if (PqsqlWrapper.PQsendQueryParams(pc, pq, num_param, ptyps, pvals, plens, pfrms, 1) == 0)
						return false;
				}
				
				if (PqsqlWrapper.PQsetSingleRowMode(pc) == 0)
					return false;
			}

			// start with a new result set
			Init(ConnectionState.Executing);

			return true;
		}

		#endregion
	}
}
