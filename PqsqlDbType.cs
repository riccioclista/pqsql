using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pqsql
{
	/// <summary>
	/// Represents a PostgreSQL data type that can be written or read to the database.
	/// Used in places such as <see cref="PqsqlParameter.PqsqlDbType"/> to unambiguously specify
	/// how to encode or decode values.
	/// </summary>
	/// <remarks>See http://www.postgresql.org/docs/current/static/datatype.html</remarks>
	public enum PqsqlDbType
	{
		// Note that it's important to never change the numeric values of this enum, since user applications
		// compile them in.

		// http://www.postgresql.org/docs/current/static/datatype-numeric.html

		#region Numeric Types

		/// <summary>
		/// Corresponds to the PostgreSQL 8-byte "bigint" type.
		/// </summary>
		/// <remarks>See </remarks>
		Int8 = 20,

		/// <summary>
		/// Corresponds to the PostgreSQL 8-byte floating-point "double" type.
		/// </summary>
		Float8 = 701,

		/// <summary>
		/// Corresponds to the PostgreSQL 4-byte "integer" type.
		/// </summary>
		Int4 = 23,

		/// <summary>
		/// Corresponds to the PostgreSQL arbitrary-precision "numeric" type.
		/// </summary>
		Numeric = 1700,

		/// <summary>
		/// Corresponds to the PostgreSQL floating-point "real" type.
		/// </summary>
		Float4 = 700,

		/// <summary>
		/// Corresponds to the PostgreSQL 2-byte "smallint" type.
		Int2 = 21,

		#endregion

		// http://www.postgresql.org/docs/9.4/static/datatype-money.html

		#region Money
		
		Money = 790,

		#endregion	

		// http://www.postgresql.org/docs/current/static/datatype-boolean.html

		#region Boolean Type

		/// <summary>
		/// Corresponds to the PostgreSQL "boolean" type.
		/// </summary>
		Boolean = 16,

		#endregion

		#region Enumerated Types

		/// <summary>
		/// Corresponds to the PostgreSQL "enum" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-enum.html</remarks>
		//Enum = 47,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-character.html

		#region Character Types

		/// <summary>
		/// Corresponds to the PostgreSQL "char(n)"type.
		/// </summary>
		Char = 18,

		/// <summary>
		/// Corresponds to the PostgreSQL "text" type.
		/// </summary>
		Text = 25,

		/// <summary>
		/// Corresponds to the PostgreSQL "varchar" type.
		/// </summary>
		Varchar = 1043,

		/// <summary>
		/// Corresponds to the PostgreSQL internal "name" type.
		/// </summary>
		Name = 19,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-binary.html

		#region Binary Data Types

		/// <summary>
		/// Corresponds to the PostgreSQL "bytea" type, holding a raw byte string.
		/// </summary>
		Bytea = 17,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-datetime.html

		#region Date/Time Types

		/// <summary>
		/// date
		/// </summary>
		Date = 1082,

		/// <summary>
		/// time
		/// </summary>
		Time = 1083,

		/// <summary>
		/// timestamp
		/// </summary>
		Timestamp = 1114,

		/// <summary>
		/// timestamp with time zone
		/// </summary>
		TimestampTZ = 1184,

		/// <summary>
		/// interval
		/// </summary>
		Interval = 1186,

		/// <summary>
		/// time with time zone
		/// </summary>
		TimeTZ = 1266,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-net-types.html

		#region Network Address Types

		/// <summary>
		/// inet
		/// </summary>
		/// <remarks>See </remarks>
		Inet = 869,

		/// <summary>
		/// cidr
		/// </summary>
		Cidr = 650,

		/// <summary>
		/// macaddr
		/// </summary>
		MacAddr = 829,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-bit.html

		#region Bit String Types

		/// <summary>
		/// bit
		/// </summary>
		/// <remarks>See </remarks>
		Bit = 1560,

		/// <summary>
		/// varbit
		/// </summary>
		Varbit = 1562,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-uuid.html

		#region UUID Type

		/// <summary>
		/// uuid
		/// </summary>
		Uuid = 2950,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-xml.html

		#region XML Type

		/// <summary>
		/// xml
		/// </summary>
		Xml = 142,

		#endregion

		#region Arrays

		/// <summary>
		/// Corresponds to the PostgreSQL "array" type, a variable-length multidimensional array of
		/// another type. This value must be combined with another value from <see cref="NpgsqlDbType"/>
		/// via a bit OR (e.g. NpgsqlDbType.Array | NpgsqlDbType.Integer)
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/arrays.html</remarks>
		//Array = int.MinValue,

		#endregion

		#region Composite Types

		/// <summary>
		/// Corresponds to the PostgreSQL "composite" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/rowtypes.html</remarks>
		//Composite = 48,

		#endregion

		#region Range Types

		/// <summary>
		/// Corresponds to the PostgreSQL "array" type, a variable-length multidimensional array of
		/// another type. This value must be combined with another value from <see cref="NpgsqlDbType"/>
		/// via a bit OR (e.g. NpgsqlDbType.Array | NpgsqlDbType.Integer)
		/// </summary>
		/// <remarks>
		/// Supported since PostgreSQL 9.2.
		/// See http://www.postgresql.org/docs/9.2/static/rangetypes.html
		/// </remarks>
		//Range = 0x40000000,

		#endregion

		#region Internal Types

		/// <summary>
		/// Corresponds to the PostgreSQL "refcursor" type.
		/// </summary>
		Refcursor = 1790,

		/// <summary>
		/// Corresponds to the PostgreSQL internal "oidvector" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-oid.html</remarks>
		//Oidvector = 29,

		/// <summary>
		/// Corresponds to the PostgreSQL "oid" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-oid.html</remarks>
		Oid = 26,

		/// <summary>
		/// Corresponds to the PostgreSQL "xid" type, an internal transaction identifier.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-oid.html</remarks>
		//Xid = 42,

		/// <summary>
		/// Corresponds to the PostgreSQL "cid" type, an internal command identifier.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-oid.html</remarks>
		//Cid = 43,

		/// <summary>
		/// Corresponds to the PostgreSQL "regtype" type, a numeric (OID) ID of a type in the pg_type table.
		/// </summary>
		//Regtype = 49,

		#endregion

		#region Special

		/// <summary>
		/// A special value that can be used to send parameter values to the database without
		/// specifying their type, allowing the database to cast them to another value based on context.
		/// The value will be converted to a string and send as text.
		/// </summary>
		/// <remarks>
		/// This value shouldn't ordinarily be used, and makes sense only when sending a data type
		/// unsupported by Npgsql.
		/// </remarks>
		Unknown = 705,

		#endregion

	}

}
