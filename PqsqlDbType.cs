using System;

namespace Pqsql
{
	/// <summary>
	/// Oid values for PostgreSQL data types
	/// </summary>
	/// <remarks>
	/// See http://www.postgresql.org/docs/current/static/datatype.html and
	/// https://www.postgresql.org/docs/current/static/catalog-pg-type.html
	/// </remarks>
	[Flags]
	public enum PqsqlDbType
	{
		// http://www.postgresql.org/docs/current/static/datatype-numeric.html

		#region Numeric types (typcategory code 'N')

		/// <summary>
		/// ~18 digit integer, 8-byte storage
		/// </summary>
		Int8 = 20,

		/// <summary>
		/// double-precision floating point number, 8-byte storage
		/// </summary>
		Float8 = 701,

		/// <summary>
		/// -2 billion to 2 billion integer, 4-byte storage
		/// </summary>
		Int4 = 23,

		/// <summary>
		/// numeric(precision, decimal), arbitrary precision number
		/// </summary>
		Numeric = 1700,

		/// <summary>
		/// single-precision floating point number, 4-byte storage
		/// </summary>
		Float4 = 700,

		/// <summary>
		/// -32 thousand to 32 thousand, 2-byte storage
		/// </summary>
		Int2 = 21,

		/// <summary>
		/// object identifier(oid), maximum 4 billion
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-oid.html
		/// </remarks>
		Oid = 26,

		/// <summary>
		/// monetary amounts, $d,ddd.cc
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-money.html
		/// </remarks>
		Cash = 790,

		/// <summary>
		/// TODO registered procedure (with args)
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-oid.html
		/// </remarks>
		//Regprocedur = 2202,

		/// <summary>
		/// TODO registered class
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-oid.html
		/// </remarks>
		//Regclass = 2205,

		/// <summary>
		/// TODO registered type
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-oid.html
		/// </remarks>
		//Regtype = 2206,

		#endregion	

		// http://www.postgresql.org/docs/current/static/datatype-boolean.html

		#region Boolean types (typcategory code 'B')

		/// <summary>
		/// boolean, 'true'/'false'
		/// </summary>
		Boolean = 16,

		#endregion

		// https://www.postgresql.org/docs/current/static/datatype-enum.html

		#region Enum types (typcategory code 'E')

		/// <summary>
		/// TODO Enumerated (enum) types are data types that comprise a static, ordered set of values.
		/// CREATE TYPE mood AS ENUM ('sad', 'ok', 'happy');
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/datatype-enum.html</remarks>
		//Enum,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-character.html

		#region String types (typcategory code 'S')

		/// <summary>
		/// char(length), blank-padded string, fixed storage length
		/// </summary>
		BPChar = 1042,

		/// <summary>
		/// variable-length string, no limit specified
		/// </summary>
		Text = 25,

		/// <summary>
		/// varchar(length), non-blank-padded string, variable storage length
		/// </summary>
		Varchar = 1043,

		/// <summary>
		/// 63-byte type for storing system identifiers
		/// </summary>
		Name = 19,

		/// <summary>
		/// single character
		/// </summary>
		Char = 18,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-datetime.html

		#region Date/time types (typcategory code 'D')

		/// <summary>
		/// date
		/// </summary>
		Date = 1082,

		/// <summary>
		/// time of day
		/// </summary>
		Time = 1083,

		/// <summary>
		/// date and time
		/// </summary>
		Timestamp = 1114,

		/// <summary>
		/// date and time with time zone
		/// </summary>
		TimestampTZ = 1184,

		/// <summary>
		/// time of day with time zone
		/// </summary>
		TimeTZ = 1266,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-datetime.html

		#region Timespan types (typcategory code 'T')

		/// <summary>
		/// @ "number" "units", time interval
		/// </summary>
		Interval = 1186,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-net-types.html

		#region Network address types (typcategory code 'I')

		/// <summary>
		/// IP address/netmask, host address, netmask optional
		/// </summary>
		/// <remarks>See </remarks>
		Inet = 869,

		/// <summary>
		/// network IP address/netmask, network address
		/// </summary>
		Cidr = 650,

		#endregion

		// http://www.postgresql.org/docs/current/static/datatype-bit.html

		#region Bit-string types (typcategory code 'V')

		/// <summary>
		/// fixed-length bit string
		/// </summary>
		Bit = 1560,

		/// <summary>
		/// variable-length bit string
		/// </summary>
		Varbit = 1562,

		#endregion

		// http://www.postgresql.org/docs/current/static/arrays.html

		#region Array types (typcategory code 'A')

		/// <summary>
		/// PostgreSQL allows columns of a table to be defined as variable-length
		/// multidimensional arrays. Arrays of any built-in or user-defined base
		/// type, enum type, or composite type can be created.
		/// 
		/// This value must be combined with another value from <see cref="PqsqlDbType"/>
		/// via a bit OR (e.g. PqsqlDbType.Array | PqsqlDbType.Integer)
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/arrays.html
		/// </remarks>
		Array = int.MinValue,

		BooleanArray = 1000,
		ByteaArray = 1001,
		CharArray = 1002,
		NameArray = 1003,
		Int2Array =	1005,
		Int4Array =	1007,
		TextArray = 1009,
		VarcharArray = 1015,
		Int8Array = 1016,
		Float4Array = 1021,
		Float8Array = 1022,
		OidArray = 1028,
		TimestampArray = 1115,
		DateArray = 1182,
		TimeArray = 1183,
		TimestampTZArray = 1185,
		IntervalArray = 1187,
		NumericArray = 1231,
		TimeTZArray = 1270,

		/// <summary>
		/// TODO array of oids, used in system tables
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-oid.html
		/// </remarks>
		Int2Vector = 22,

		/// <summary>
		/// TODO array of oids, used in system tables
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-oid.html
		/// </remarks>
		OidVector = 30,

		#endregion

		// https://www.postgresql.org/docs/current/static/rowtypes.html

		#region Composite types (typcategory code 'C')

		/// <summary>
		/// TODO A composite type represents the structure of a row or record; it is essentially just a list of field names and their data types.
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/rowtypes.html</remarks>
		//Composite,

		#endregion

		// http://www.postgresql.org/docs/current/static/rangetypes.html

		#region Range types (typcategory code 'R')

		/// <summary>
		/// TODO Range types are data types representing a range of values of some element type (called the range's subtype).
		/// </summary>
		/// <remarks>See http://www.postgresql.org/docs/current/static/rangetypes.html</remarks>
		//Range,

		#endregion

		// https://www.postgresql.org/docs/current/static/xtypes.html

		#region User-defined types (typcategory code 'U')

		/// <summary>
		/// variable-length string, binary values escaped
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-binary.html
		/// </remarks>
		Bytea = 17,

		/// <summary>
		/// JSON data types are for storing JSON (JavaScript Object Notation) data,
		/// as specified in RFC 7159.
		/// </summary>
		/// <remarks>
		/// See https://www.postgresql.org/docs/current/static/datatype-json.html
		/// </remarks>
		Json = 114,

		/// <summary>
		/// Binary JSON
		/// </summary>
		/// <remarks>
		/// See https://www.postgresql.org/docs/current/static/datatype-json.html
		/// </remarks>
		Jsonb = 3802,

		/// <summary>
		/// reference to cursor (portal name)
		/// </summary>
		Refcursor = 1790,

		/// <summary>
		/// XX:XX:XX:XX:XX:XX, MAC address
		/// </summary>
		/// <remarks>
		/// See https://www.postgresql.org/docs/current/static/datatype-net-types.html
		/// </remarks>
		MacAddr = 829,

		/// <summary>
		/// uuid
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-uuid.html
		/// </remarks>
		Uuid = 2950,

		/// <summary>
		/// xml
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-xml.html
		/// </remarks>
		Xml = 142,

		/// <summary>
		/// TODO (block, offset), physical location of tuple
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-oid.html
		/// </remarks>
		Tid = 27,
	
		/// <summary>
		/// TODO transaction id
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-oid.html
		/// </remarks>
		Xid = 28,

		/// <summary>
		/// TODO command identifier type, sequence in transaction id
		/// </summary>
		/// <remarks>
		/// See http://www.postgresql.org/docs/current/static/datatype-oid.html
		/// </remarks>
		Cid = 29,

		#endregion

		#region unknown type (typcategory code 'X')

		/// <summary>
		/// A special value that can be used to send parameter values to the database without
		/// specifying their type, allowing the database to cast them to another value based on context.
		/// The value will be converted to a string and send as text.
		/// </summary>
		Unknown = 705,

		#endregion

		// https://www.postgresql.org/docs/current/static/datatype-pseudo.html

		#region Pseudo-types (typcategory code 'P')

		/// <summary>
		/// Indicates that a function returns no value.
		/// </summary>
		Void = 2278,

		#endregion

	}

}
