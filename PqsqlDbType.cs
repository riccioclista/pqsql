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

		#region Numeric Types

		/// <summary>
		/// Corresponds to the PostgreSQL 8-byte "bigint" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
		Bigint = 1,

		/// <summary>
		/// Corresponds to the PostgreSQL 8-byte floating-point "double" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
		Double = 8,

		/// <summary>
		/// Corresponds to the PostgreSQL 4-byte "integer" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
		Integer = 9,

		/// <summary>
		/// Corresponds to the PostgreSQL arbitrary-precision "numeric" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
		Numeric = 13,

		/// <summary>
		/// Corresponds to the PostgreSQL floating-point "real" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
		Real = 17,

		/// <summary>
		/// Corresponds to the PostgreSQL 2-byte "smallint" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-numeric.html</remarks>
		Smallint = 18,

		#endregion

		#region Boolean Type

		/// <summary>
		/// Corresponds to the PostgreSQL "boolean" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-boolean.html</remarks>
		Boolean = 2,

		#endregion

		#region Enumerated Types

		/// <summary>
		/// Corresponds to the PostgreSQL "enum" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-enum.html</remarks>
		Enum = 47,

		#endregion


		#region Character Types

		/// <summary>
		/// Corresponds to the PostgreSQL "char(n)"type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-character.html</remarks>
		Char = 6,

		/// <summary>
		/// Corresponds to the PostgreSQL "text" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-character.html</remarks>
		Text = 19,

		/// <summary>
		/// Corresponds to the PostgreSQL "varchar" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-character.html</remarks>
		Varchar = 22,

		/// <summary>
		/// Corresponds to the PostgreSQL internal "name" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-character.html</remarks>
		Name = 32,

		#endregion

		#region Binary Data Types

		/// <summary>
		/// Corresponds to the PostgreSQL "bytea" type, holding a raw byte string.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-binary.html</remarks>
		Bytea = 4,

		#endregion

		#region Date/Time Types

		/// <summary>
		/// Corresponds to the PostgreSQL "date" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
		Date = 7,

		/// <summary>
		/// Corresponds to the PostgreSQL "time" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
		Time = 20,

		/// <summary>
		/// Corresponds to the PostgreSQL "timestamp" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
		Timestamp = 21,

		/// <summary>
		/// Corresponds to the PostgreSQL "timestamp with time zone" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
		TimestampTZ = 26,

		/// <summary>
		/// Corresponds to the PostgreSQL "interval" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
		Interval = 30,

		/// <summary>
		/// Corresponds to the PostgreSQL "time with time zone" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-datetime.html</remarks>
		TimeTZ = 31,

		#endregion

		#region Network Address Types

		/// <summary>
		/// Corresponds to the PostgreSQL "inet" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-net-types.html</remarks>
		Inet = 24,

		/// <summary>
		/// Corresponds to the PostgreSQL "cidr" type, a field storing an IPv4 or IPv6 network.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-net-types.html</remarks>
		Cidr = 44,

		/// <summary>
		/// Corresponds to the PostgreSQL "macaddr" type, a field storing a 6-byte physical address.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-net-types.html</remarks>
		MacAddr = 34,

		#endregion

		#region Bit String Types

		/// <summary>
		/// Corresponds to the PostgreSQL "bit" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-bit.html</remarks>
		Bit = 25,

		/// <summary>
		/// Corresponds to the PostgreSQL "varbit" type, a field storing a variable-length string of bits.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-boolean.html</remarks>
		Varbit = 39,

		#endregion

		#region UUID Type

		/// <summary>
		/// Corresponds to the PostgreSQL "uuid" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-uuid.html</remarks>
		Uuid = 27,

		#endregion

		#region Arrays

		/// <summary>
		/// Corresponds to the PostgreSQL "array" type, a variable-length multidimensional array of
		/// another type. This value must be combined with another value from <see cref="NpgsqlDbType"/>
		/// via a bit OR (e.g. NpgsqlDbType.Array | NpgsqlDbType.Integer)
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/arrays.html</remarks>
		Array = int.MinValue,

		#endregion

		#region Composite Types

		/// <summary>
		/// Corresponds to the PostgreSQL "composite" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/rowtypes.html</remarks>
		Composite = 48,

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
		Range = 0x40000000,

		#endregion

		#region Internal Types

		/// <summary>
		/// Corresponds to the PostgreSQL "refcursor" type.
		/// </summary>
		Refcursor = 23,

		/// <summary>
		/// Corresponds to the PostgreSQL internal "oidvector" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-oid.html</remarks>
		Oidvector = 29,

		/// <summary>
		/// Corresponds to the PostgreSQL "oid" type.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-oid.html</remarks>
		Oid = 41,

		/// <summary>
		/// Corresponds to the PostgreSQL "xid" type, an internal transaction identifier.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-oid.html</remarks>
		Xid = 42,

		/// <summary>
		/// Corresponds to the PostgreSQL "cid" type, an internal command identifier.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-oid.html</remarks>
		Cid = 43,

		/// <summary>
		/// Corresponds to the PostgreSQL "regtype" type, a numeric (OID) ID of a type in the pg_type table.
		/// </summary>
		Regtype = 49,

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
		Unknown = 40,

		#endregion

	}
}
