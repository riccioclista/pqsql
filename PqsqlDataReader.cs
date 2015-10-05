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
	internal class PqsqlColInformation
	{
		public PqsqlDbType Oid { get; set; }
		public int Format { get; set; }
		public int Modifier { get; set; }
		public int Size { get; set; }
		public string Name { get; set; }
	};



	internal static class PqsqlTypeNames
	{
		class PqsqlTypeName
		{
			public string Name {	get; set; }
			public Type Type { get;	set; }
			public Func<IntPtr,int,int,object> GetValue	{	get;s et; }
		}

		/// <summary>
		/// Static string Dictionary example
		/// </summary>
		static Dictionary<PqsqlDbType, PqsqlTypeName> mDict = new Dictionary<PqsqlDbType, PqsqlTypeName>
    {
			{ PqsqlDbType.Boolean, new PqsqlTypeName { Name="bool", Type=typeof(bool), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetBoolean(res,row,ord); } } },
			{ PqsqlDbType.Float8, new PqsqlTypeName { Name="float8", Type=typeof(double), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDouble(res,row,ord); } } },
			{ PqsqlDbType.Int4, new PqsqlTypeName { Name="int4", Type=typeof(int), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetInt32(res,row,ord); } } },
			{ PqsqlDbType.Numeric, new PqsqlTypeName { Name="numeric", Type=typeof(Decimal), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDecimal(res,row,ord); } } },
			{ PqsqlDbType.Float4, new PqsqlTypeName { Name="float4", Type=typeof(float), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetFloat(res,row,ord); } } },
			{ PqsqlDbType.Int2, new PqsqlTypeName { Name="int2", Type=typeof(short), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetInt16(res,row,ord); } } },
			{ PqsqlDbType.Char, new PqsqlTypeName { Name="char", Type=typeof(char), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetByte(res,row,ord); } } },
			{ PqsqlDbType.Text, new PqsqlTypeName { Name="text", Type=typeof(string), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetString(res,row,ord); } } },
			{ PqsqlDbType.Varchar, new PqsqlTypeName { Name="varchar", Type=typeof(string), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetString(res,row,ord); } } },
			{ PqsqlDbType.Name, new PqsqlTypeName { Name="name", Type=typeof(string), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetString(res,row,ord); } } },
			{ PqsqlDbType.Bytea, new PqsqlTypeName { Name="bytea", Type=typeof(byte[]), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetBytes(res,row,ord); } } },
			{ PqsqlDbType.Date, new PqsqlTypeName { Name="date", Type=typeof(DateTime), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDate(res,row,ord); } } },
			{ PqsqlDbType.Time, new PqsqlTypeName { Name="time", Type=typeof(DateTime), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetTime(res,row,ord); } } },
			{ PqsqlDbType.Timestamp, new PqsqlTypeName { Name="timestamp", Type=typeof(DateTime), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDateTime(res,row,ord); } } },
			{ PqsqlDbType.TimestampTZ, new PqsqlTypeName { Name="timestamptz", Type=typeof(DateTime), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDateTime(res,row,ord); } } },
			{ PqsqlDbType.Interval, new PqsqlTypeName { Name="interval", Type=typeof(TimeSpan), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDateTime(res,row,ord); } } },
			{ PqsqlDbType.TimeTZ, new PqsqlTypeName { Name="timetz", Type=typeof(DateTime), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDateTime(res,row,ord); } } },
			//{ PqsqlDbType.Inet, new PqsqlTypeName { Name="inet", Type=typeof() } },
			//{ PqsqlDbType.Cidr, new PqsqlTypeName { Name="cidr", Type=typeof() } },
			//{ PqsqlDbType.MacAddr, new PqsqlTypeName { Name="macaddr", Type=typeof() } },
			//{ PqsqlDbType.Bit, new PqsqlTypeName { Name="bit", Type=typeof() } },
			//{ PqsqlDbType.Varbit, new PqsqlTypeName { Name="varbit", Type=typeof() } },
			{ PqsqlDbType.Uuid, new PqsqlTypeName { Name="uuid", Type=typeof(Guid), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetGuid(res,row,ord); } } },
			//{ PqsqlDbType.Refcursor, new PqsqlTypeName { Name="refcursor", Type=typeof() } },
			{ PqsqlDbType.Oid, new PqsqlTypeName { Name="oid", Type=typeof(uint), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetInt32(res,row,ord); } } },
			{ PqsqlDbType.Unknown, new PqsqlTypeName { Name="unknown", Type=typeof(string), GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetString(res,row,ord); } } }
    };


		static PqsqlTypeName Get(PqsqlDbType oid)
		{
			PqsqlTypeName result;
			if (mDict.TryGetValue(oid, out result))
			{
				return result;
			}
			throw new NotSupportedException("Datatype not supported yet");
		}

		public static string GetName(PqsqlDbType oid)
		{
			return Get(oid).Name;
		}


		public static Type GetType(PqsqlDbType oid)
		{
			return Get(oid).Type;
		}


		public static Func<IntPtr, int, int, object> GetValue(PqsqlDbType oid)
		{
			return Get(oid).GetValue;
		}
	}




	public class PqsqlDataReader : DbDataReader
	{
		/// <summary>
		/// PGresult*
		/// </summary>
		IntPtr mResult;

		PqsqlCommand mCmd;

		CommandBehavior mBehaviour;

		// row information of current result set
		PqsqlColInformation[] mRowInformation;

		// -1: nothing read yet, 0: first row, ...
		int mRownum;

		int mStmtNum;
		string[] mStatements;


		#region DbDataReader

		// Summary:
		//     Initializes a new instance of the PqsqlDataReader class.
		public PqsqlDataReader(PqsqlCommand command, CommandBehavior behavior, string[] statements)
		{
			mCmd = command;
			mBehaviour = behavior;

			mRowInformation = null;
			mRownum = -1;

			int n = statements.Length;
			mStatements = new string[n];
			Array.Copy(statements, mStatements, n);
		}

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

				return mRowInformation.Length;
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
				if (mResult == IntPtr.Zero)
					throw new NotSupportedException("No result read yet");

				return PqsqlWrapper.PQntuples(mResult) > 0;
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
				switch (s)
				{
					case ConnectionState.Closed:
						return true;

					case ConnectionState.Open:
					case ConnectionState.Executing:
					case ConnectionState.Fetching:
						return false;

					default:
						throw new InvalidOperationException("Connect state " + s.ToString());
				}
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
					throw new InvalidOperationException("No data read yet.");

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

		}

		//
		// Summary:
		//     Releases all resources used by the current instance of the System.Data.Common.DbDataReader
		//     class.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void Dispose();
		//
		// Summary:
		//     Releases the managed resources used by the System.Data.Common.DbDataReader
		//     and optionally releases the unmanaged resources.
		//
		// Parameters:
		//   disposing:
		//     true to release managed and unmanaged resources; false to release only unmanaged
		//     resources.
		protected virtual void Dispose(bool disposing);



		protected void CheckOrdinal(int ordinal)
		{
			if (mResult == IntPtr.Zero)
				throw new IndexOutOfRangeException("No result read yet.");

			if (ordinal < 0 || ordinal >= FieldCount)
				throw new IndexOutOfRangeException("Column " + ordinal.ToString() + " out of range");
		}

		protected void CheckOrdinalType(int ordinal, PqsqlDbType oid)
		{
			if (mResult == IntPtr.Zero)
				throw new IndexOutOfRangeException("No result read yet.");

			if (ordinal < 0 || ordinal >= FieldCount)
				throw new IndexOutOfRangeException("Column " + ordinal.ToString() + " out of range");

			if (oid != mRowInformation[ordinal].Oid)
				throw new InvalidCastException("Wrong datatype", (int)oid);
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
			return GetBoolean(mResult, mRownum, ordinal);
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
		public abstract byte GetByte(int ordinal)
		{
			CheckOrdinal(ordinal); // oid does not matter
			return GetByte(mResult, mRownum, ordinal);
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
		public abstract long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length);
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
			return PqsqlTypeNames.GetName(mRowInformation[ordinal].Oid);
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
			return GetDateTime(mResult, mRownum, ordinal);
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
			return GetDecimal(mResult, mRownum, ordinal);
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
			return GetDouble(mResult, mRownum, ordinal);
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
		public abstract Type GetFieldType(int ordinal)
		{
			CheckOrdinal(ordinal);
			return PqsqlTypeNames.GetType(mRowInformation[ordinal].Oid);
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
			return GetFloat(mResult, mRownum, ordinal);
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
			return GetGuid(mResult, mRownum, ordinal);
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
			return GetInt16(mResult, mRownum, ordinal);
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
			return GetInt32(mResult, mRownum, ordinal);
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
		public abstract long GetInt64(int ordinal)
		{
			CheckOrdinalType(ordinal, PqsqlDbType.Int8);
			return GetInt64(mResult, mRownum, ordinal);
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
			return mRowInformation[ordinal].Name;
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
				throw new IndexOutOfRangeException("Invalid column name");

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
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual Type GetProviderSpecificFieldType(int ordinal);
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
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual object GetProviderSpecificValue(int ordinal);
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
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual int GetProviderSpecificValues(object[] values);
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

			PqsqlDbType oid = mRowInformation[ordinal].Oid;
			if (oid != PqsqlDbType.Text && oid != PqsqlDbType.Varchar && oid != PqsqlDbType.Unknown)
			{
				throw new InvalidCastException("Wrong datatype", (int) oid);
			}

			IntPtr v = PqsqlWrapper.PQgetvalue(mResult, mRownum, ordinal);
			
			IntPtr utp;
			int len;
			unsafe
			{
				utp = PqsqlBinaryFormat.pqbf_get_unicode_text(v, &len);
			}
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
			return PqsqlTypeNames.GetValue(mRowInformation[ordinal].Oid)(mResult,mRownum,ordinal);
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
			return PqsqlWrapper.PQgetisnull(mResult, mRownum, ordinal) == 1;
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
			if (mStmtNum >= mStatements.Length) // finished
			{
				return false;
			}

			// TODO send next query
			mCmd.CommandText = mStatements[mStmtNum]; 
			mStmtNum++;

			// start with the next result set
			mRowInformation = null;
			mRownum = -1;

			return Read(); // TODO what if an intermediate result set is empty?
		}
		//
		// Summary:
		//     Advances the reader to the next record in a result set.
		//
		// Returns:
		//     true if there are more rows; otherwise false.
		public override bool Read()
		{
			mResult = PqsqlWrapper.PQgetResult(mCmd.Connection.PGConnection);

			if (mResult != IntPtr.Zero)
			{
				mRownum++;

				if (mRownum == 0) // first row => get new column information
				{
					int n = PqsqlWrapper.PQnfields(mResult);
					mRowInformation = new PqsqlColInformation[n];

					for (int o = 0; o < n; o++)
					{
						mRowInformation[o].Oid = (PqsqlDbType) PqsqlWrapper.PQftype(mResult, o);
						mRowInformation[o].Name = PqsqlWrapper.PQfname(mResult, o);
						mRowInformation[o].Size = PqsqlWrapper.PQfsize(mResult, o);
						mRowInformation[o].Modifier = PqsqlWrapper.PQfmod(mResult, o);
						mRowInformation[o].Format = PqsqlWrapper.PQfformat(mResult, o);
					}
				}

				return true;
			}
			
			return false;
		}

		#endregion
	}
}
