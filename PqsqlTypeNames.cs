using System;
using System.Collections.Generic;
using System.Data;

namespace Pqsql
{
	/// <summary>
	/// Static dictionary of Postgres types, datatype names, GetValue() / SetValue() / SetArrayItem() delegates
	/// </summary>
	internal static class PqsqlTypeNames
	{
		public sealed class PqsqlTypeName
		{
			public string Name { get; set; }
			public TypeCode TypeCode { get; set; }
			public Type Type { get; set; }
			public DbType DbType { get; set; }
			public PqsqlDbType ArrayDbType { get; set; }
			public Func<IntPtr, int, int, int, object> GetValue { get; set; }
			public Action<IntPtr, object> SetValue { get; set; }
			public Action<IntPtr, object> SetArrayItem { get; set; }
		}

		private static Action<IntPtr, object> setNumericArray = (a, o) =>
		{
			double d = Convert.ToDouble(o);

			long len0 = PqsqlBinaryFormat.pqbf_get_buflen(a); // get start position

			PqsqlBinaryFormat.pqbf_set_array_itemlength(a, -2); // first set an invalid item length
			PqsqlBinaryFormat.pqbf_set_numeric(a, d); // encode numeric value (variable length)

			int len = (int) (PqsqlBinaryFormat.pqbf_get_buflen(a) - len0); // get new buffer length
			// update array item length == len - 4 bytes
			PqsqlBinaryFormat.pqbf_update_array_itemlength(a, -len, len - 4);
		};

		private static Action<IntPtr, object> setText = (pb, val) =>
		{
			unsafe
			{
				fixed (char* t = (string) val)
				{
					PqsqlBinaryFormat.pqbf_add_unicode_text(pb, t);
				}
			}
		}; 

		private static Action<IntPtr, object> setTextArray = (a, o) =>
		{
			string v = (string) o;

			long len0 = PqsqlBinaryFormat.pqbf_get_buflen(a); // get start position

			PqsqlBinaryFormat.pqbf_set_array_itemlength(a, -2); // first set an invalid item length

			unsafe
			{
				fixed (char* t = v)
				{
					PqsqlBinaryFormat.pqbf_set_unicode_text(a, t); // encode text value (variable length)
				}
			}

			int len = (int) (PqsqlBinaryFormat.pqbf_get_buflen(a) - len0); // get new buffer length
			// update array item length == len - 4 bytes
			PqsqlBinaryFormat.pqbf_update_array_itemlength(a, -len, len - 4);
		};


		// maps PqsqlDbType to PqsqlTypeName
		static readonly Dictionary<PqsqlDbType, PqsqlTypeName> mPqsqlDbTypeDict = new Dictionary<PqsqlDbType, PqsqlTypeName>
    {
			{ PqsqlDbType.Boolean,
				new PqsqlTypeName { 
					Name="bool",
					TypeCode=TypeCode.Boolean,
					Type=typeof(bool),
					DbType=DbType.Boolean,
					ArrayDbType=PqsqlDbType.BooleanArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetBoolean(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_bool(pb, (bool) val ? 1 : 0),
					SetArrayItem = (a, o) =>
					{
						bool v = (bool) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 1);
						PqsqlBinaryFormat.pqbf_set_bool(a, v ? 1 : 0);
					}
				}
			},
			{ PqsqlDbType.Float8,
				new PqsqlTypeName {
					Name="float8",
					TypeCode=TypeCode.Double,
					Type=typeof(double),
					DbType=DbType.Double,
					ArrayDbType=PqsqlDbType.Float8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDouble(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_float8(pb, (double) val),
					SetArrayItem = (a, o) => {
						double v = (double) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
						PqsqlBinaryFormat.pqbf_set_float8(a, v);
					}
				}
			},
			{ PqsqlDbType.Int4,
				new PqsqlTypeName {
					Name="int4",
					TypeCode=TypeCode.Int32,
					Type=typeof(int),
					DbType=DbType.Int32,
					ArrayDbType=PqsqlDbType.Int4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt32(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_int4(pb, (int) val),
					SetArrayItem = (a, o) => {
						int v = (int) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 4);
						PqsqlBinaryFormat.pqbf_set_int4(a, v);
					}
				}
			},
			{ PqsqlDbType.Int8,
				new PqsqlTypeName {
					Name="int8",
					TypeCode=TypeCode.Int64,
					Type=typeof(long),
					DbType=DbType.Int64,
					ArrayDbType=PqsqlDbType.Int8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt64(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_int8(pb, (long) val),
					SetArrayItem = (a, o) => {
						long v = (long) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
						PqsqlBinaryFormat.pqbf_set_int8(a, v);
					}
				}
			},
			{ PqsqlDbType.Numeric,
				new PqsqlTypeName {
					Name="numeric",
					TypeCode=TypeCode.Decimal,
					Type=typeof(Decimal),
					DbType=DbType.VarNumeric,
					ArrayDbType=PqsqlDbType.NumericArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetNumeric(res,row,ord,typmod),
					SetValue=(pb, val) => {
						double d = Convert.ToDouble(val);
						PqsqlBinaryFormat.pqbf_add_numeric(pb, d);
					},
					SetArrayItem = setNumericArray
				}
			},
			{ PqsqlDbType.Float4,
				new PqsqlTypeName {
					Name="float4",
					TypeCode=TypeCode.Single,
					Type=typeof(float),
					DbType=DbType.Single,
					ArrayDbType=PqsqlDbType.Float4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetFloat(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_float4(pb, (float) val),
					SetArrayItem = (a, o) => {
						float v = (float) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 4);
						PqsqlBinaryFormat.pqbf_set_float4(a, v);
					}
				}
			},
			{ PqsqlDbType.Int2,
				new PqsqlTypeName {
					Name="int2",
					TypeCode=TypeCode.Int16,
					Type=typeof(short),
					DbType=DbType.Int16,
					ArrayDbType=PqsqlDbType.Int2Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt16(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_int2(pb, (short) val),
					SetArrayItem = (a, o) => {
						short v = (short) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 2);
						PqsqlBinaryFormat.pqbf_set_int2(a, v);
					}
				}
			},
			{ PqsqlDbType.Char,
				new PqsqlTypeName {
					Name="char",
					TypeCode=TypeCode.String,
					Type=typeof(char[]),
					DbType=DbType.StringFixedLength,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=null,
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Text,
				new PqsqlTypeName {
					Name="text",
					TypeCode=TypeCode.String,
					Type=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.TextArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.Varchar,
				new PqsqlTypeName {
					Name="varchar",
					TypeCode=TypeCode.String,
					Type=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.VarcharArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.Name,
				new PqsqlTypeName {
					Name="name",
					TypeCode=TypeCode.String,
					Type=typeof(string),
					DbType=DbType.StringFixedLength,
					ArrayDbType=PqsqlDbType.NameArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.Bytea,
				new PqsqlTypeName {
					Name="bytea",
					TypeCode=TypeCode.Object,
					Type=typeof(byte[]),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue= (res, row, ord, typmod) => {
						int n = (int) PqsqlDataReader.GetBytes(res, row, ord, 0, null, 0, 0);
						byte[] bs = new byte[n];
						n = (int) PqsqlDataReader.GetBytes(res, row, ord, 0, bs, 0, n);

						if (n != bs.Length)
							throw new IndexOutOfRangeException("Received wrong number of bytes for byte array");
				
						return bs;
					},
					SetValue= (pb, val) =>
					{
						byte[] buf = (byte[]) val;
						ulong len = (ulong) buf.LongLength;
						unsafe
						{
							fixed (byte* b = buf)
							{
								PqsqlBinaryFormat.pqbf_add_bytea(pb, (sbyte*) b, len);
							}
						}
					},
					SetArrayItem = null // TODO
				}
			},
			{ PqsqlDbType.Date,
				new PqsqlTypeName {
					Name="date",
					TypeCode=TypeCode.DateTime,
					Type=typeof(DateTime),
					DbType=DbType.Date,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDate(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_date(pb, (DateTime) val); }
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Time,
				new PqsqlTypeName {
					Name="time",
					TypeCode=TypeCode.DateTime,
					Type=typeof(DateTime),
					DbType=DbType.Time,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetTime(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_time(pb, (DateTime) val); }
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Timestamp,
				new PqsqlTypeName {
					Name="timestamp",
					TypeCode=TypeCode.DateTime,
					Type=typeof(DateTime),
					DbType=DbType.DateTime,
					ArrayDbType=PqsqlDbType.TimestampArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDateTime(res,row,ord),
					SetValue=(pb, val) => {
						DateTime dt = (DateTime) val;
						long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
						long sec = ticks / TimeSpan.TicksPerSecond;
						int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);
						PqsqlBinaryFormat.pqbf_add_timestamp(pb, sec, usec);
					},
					SetArrayItem = (a, o) => {
						DateTime dt = (DateTime) o;
						long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
						long sec = ticks / TimeSpan.TicksPerSecond;
						int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
						PqsqlBinaryFormat.pqbf_set_timestamp(a, sec, usec);
					}
				}
			},
			{ PqsqlDbType.TimestampTZ,
				new PqsqlTypeName {
					Name="timestamptz",
					TypeCode=TypeCode.DateTime,
					Type=typeof(DateTime),
					DbType=DbType.DateTimeOffset,
					ArrayDbType=PqsqlDbType.TimestampTZArray, // TODO timezone?
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDateTime(res,row,ord),
					SetValue=(pb, val) => { // TODO timezone?
						DateTime dt = (DateTime) val;
						long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
						long sec = ticks / TimeSpan.TicksPerSecond;
						int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);
						PqsqlBinaryFormat.pqbf_add_timestamp(pb, sec, usec);
					},
					SetArrayItem = (a, o) => { // TODO timezone?
						DateTime dt = (DateTime) o;
						long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
						long sec = ticks / TimeSpan.TicksPerSecond;
						int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
						PqsqlBinaryFormat.pqbf_set_timestamp(a, sec, usec);
					}
				}
			},
			{ PqsqlDbType.Interval,
				new PqsqlTypeName {
					Name="interval",
					TypeCode=TypeCode.DateTime,
					Type=typeof(TimeSpan),
					DbType=DbType.DateTime,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInterval(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_interval(pb, (DateTime) val); }
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.TimeTZ,
				new PqsqlTypeName {
					Name="timetz",
					TypeCode=TypeCode.DateTime,
					Type=typeof(DateTime),
					DbType=DbType.DateTime,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetTime(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_timetz(pb, (DateTime) val); }
					SetArrayItem = null
				}
			},
			//{ PqsqlDbType.Inet, new PqsqlTypeName { Name="inet", Type=typeof() } },
			//{ PqsqlDbType.Cidr, new PqsqlTypeName { Name="cidr", Type=typeof() } },
			//{ PqsqlDbType.MacAddr, new PqsqlTypeName { Name="macaddr", Type=typeof() } },
			//{ PqsqlDbType.Bit, new PqsqlTypeName { Name="bit", Type=typeof() } },
			//{ PqsqlDbType.Varbit, new PqsqlTypeName { Name="varbit", Type=typeof() } },
			{ PqsqlDbType.Uuid,
				new PqsqlTypeName {
					Name="uuid",
					TypeCode=TypeCode.Object,
					Type=typeof(Guid),
					DbType=DbType.Guid,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetGuid(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_uuid(pb, (Guid) val); }
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Refcursor,
				new PqsqlTypeName {
					Name="refcursor",
					TypeCode=TypeCode.String,
					Type=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.Oid,
				new PqsqlTypeName {
					Name="oid",
					TypeCode=TypeCode.UInt32,
					Type=typeof(uint),
					DbType=DbType.UInt32,
					ArrayDbType=PqsqlDbType.OidArray,
					GetValue=(res, row, ord, typmod) => (uint) PqsqlDataReader.GetInt32(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_oid(pb, (uint) val),
					SetArrayItem = (a, o) => {
						uint v = (uint) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 4);
						PqsqlBinaryFormat.pqbf_set_oid(a, v);
					}
				}
			},
			{ PqsqlDbType.Unknown,
				new PqsqlTypeName {
					Name="unknown",
					TypeCode=TypeCode.String,
					Type=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = null
				}
			},


			{ PqsqlDbType.BooleanArray,
				new PqsqlTypeName {
					Name="_bool",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.BooleanArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Boolean, typeof(bool?), typeof(bool), (x, len) => PqsqlBinaryFormat.pqbf_get_bool(x) > 0),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Int2Array,
				new PqsqlTypeName {
					Name="_int2",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Int2Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Int2, typeof(short?), typeof(short), (x, len) => PqsqlBinaryFormat.pqbf_get_int2(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Int4Array,
				new PqsqlTypeName {
					Name="_int4",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Int4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Int4, typeof(int?), typeof(int), (x, len) => PqsqlBinaryFormat.pqbf_get_int4(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},

			{ PqsqlDbType.TextArray,
				new PqsqlTypeName {
					Name="_text",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.TextArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Text, typeof(string), typeof(string), PqsqlDataReader.GetStringValue),
					SetValue = null,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.NameArray,
				new PqsqlTypeName {
					Name="_name",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.NameArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Name, typeof(string), typeof(string), PqsqlDataReader.GetStringValue),
					SetValue = null,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.VarcharArray,
				new PqsqlTypeName {
					Name="_varchar",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.VarcharArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Varchar, typeof(string), typeof(string), PqsqlDataReader.GetStringValue),
					SetValue = null,
					SetArrayItem = setTextArray
				}
			},

			{ PqsqlDbType.Int8Array,
				new PqsqlTypeName {
					Name="_int8",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Int8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Int8, typeof(long?), typeof(long), (x, len) => PqsqlBinaryFormat.pqbf_get_int8(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Float4Array,
				new PqsqlTypeName {
					Name="_float4",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Float4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Float4, typeof(float?), typeof(float), (x, len) => PqsqlBinaryFormat.pqbf_get_float4(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Float8Array,
				new PqsqlTypeName {
					Name="_float8",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Float8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Float8, typeof(double?), typeof(double), (x, len) => PqsqlBinaryFormat.pqbf_get_float8(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.OidArray,
				new PqsqlTypeName {
					Name="_oid",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.OidArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Oid, typeof(uint?), typeof(uint), (x, len) => (uint) PqsqlBinaryFormat.pqbf_get_int4(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.TimestampArray,
				new PqsqlTypeName {
					Name="_timestamp",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.TimestampArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Timestamp, typeof(DateTime?), typeof(DateTime), (x, len) => {
						long sec;
						int usec;
						unsafe { PqsqlBinaryFormat.pqbf_get_timestamp(x, &sec, &usec); }
						long ticks = PqsqlBinaryFormat.UnixEpochTicks + sec * TimeSpan.TicksPerSecond + usec * 10;
						DateTime dt = new DateTime(ticks);
						return dt;
					}),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.TimestampTZArray,
				new PqsqlTypeName {
					Name="_timestamptz",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.TimestampTZArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Timestamp, typeof(DateTime?), typeof(DateTime), (x, len) => { // TODO timezone?
						long sec;
						int usec;
						unsafe { PqsqlBinaryFormat.pqbf_get_timestamp(x, &sec, &usec); }
						long ticks = PqsqlBinaryFormat.UnixEpochTicks + sec * TimeSpan.TicksPerSecond + usec * 10;
						DateTime dt = new DateTime(ticks);
						return dt;
					}),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.NumericArray,
				new PqsqlTypeName {
					Name="_numeric",
					TypeCode=TypeCode.Object,
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.NumericArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Numeric, typeof(double?), typeof(double), (x, len) => PqsqlBinaryFormat.pqbf_get_numeric(x,typmod)),
					SetValue=null,
					SetArrayItem = null
				}
			},

    };

		// maps DbType to PqsqlDbType (DbType.DateTimeOffset is maximal integer for the array)
		static readonly PqsqlDbType[] mDbTypeArray = new PqsqlDbType[(int) DbType.DateTimeOffset + 1]
		{
			// DbType.AnsiString = 0,
			PqsqlDbType.Text,

			// DbType.Binary = 1,
			PqsqlDbType.Unknown,

			// DbType.Byte = 2,
			PqsqlDbType.Int2,

			// DbType.Boolean = 3,
			PqsqlDbType.Boolean,

			// DbType.Currency = 4,
			PqsqlDbType.Money,
			
			// DbType.Date = 5,
			PqsqlDbType.Date,

			// DbType.DateTime = 6,
			PqsqlDbType.Timestamp,

			// DbType.Decimal = 7,
			PqsqlDbType.Numeric,
			
			// DbType.Double = 8,
			PqsqlDbType.Float8,

			// DbType.Guid = 9,
			PqsqlDbType.Uuid,
		
			//DbType.Int16 = 10,
			PqsqlDbType.Int2,

			//DbType.Int32 = 11,
			PqsqlDbType.Int4,

			//DbType.Int64 = 12,
			PqsqlDbType.Int8,

			//DbType.Object = 13,
			PqsqlDbType.Unknown,

			//DbType.SByte = 14,
			PqsqlDbType.Int2,

			//DbType.Single = 15,
			PqsqlDbType.Float4,

			//DbType.String = 16,
			PqsqlDbType.Text,

			//DbType.Time = 17,
			PqsqlDbType.Time,

			//DbType.UInt16 = 18,
			PqsqlDbType.Int2,

			//DbType.UInt32 = 19,
			PqsqlDbType.Int4,

			//DbType.UInt64 = 20,
			PqsqlDbType.Int8,

			//DbType.VarNumeric = 21,
			PqsqlDbType.Numeric,

			//DbType.AnsiStringFixedLength = 22,
			PqsqlDbType.Varchar,

			//DbType.StringFixedLength = 23,
			PqsqlDbType.Varchar,

			// Dbtype=24 does not exist!
			PqsqlDbType.Unknown,

			//DbType.Xml = 25,
			PqsqlDbType.Xml,

			//DbType.DateTime2 = 26,
			PqsqlDbType.Timestamp,

			//DbType.DateTimeOffset = 27,
			PqsqlDbType.TimestampTZ
			
		};

		// for PqsqlDataReader
		public static PqsqlTypeName Get(PqsqlDbType oid)
		{
			PqsqlTypeName result;
			if (mPqsqlDbTypeDict.TryGetValue(oid, out result))
			{
				return result;
			}
			throw new NotSupportedException("Datatype not supported");
		}

		// for PqsqlParameter
		public static PqsqlDbType GetPqsqlDbType(DbType dbType)
		{
			return mDbTypeArray[(int) dbType];
		}

		// for PqsqlParameter
		public static DbType GetDbType(PqsqlDbType oid)
		{
			return Get(oid).DbType;
		}

		// for PqsqlParameterCollection
		public static Action<IntPtr, object> SetArrayValue(PqsqlDbType oid, PqsqlTypeName n)
		{
			oid &= ~PqsqlDbType.Array; // remove Array flag
			PqsqlDbType arrayoid = n.ArrayDbType;
			Action<IntPtr, object> setArrayItem = n.SetArrayItem;

			// return closure
			return (pb, val) =>
			{
				Array aparam = val as Array;
				int rank = aparam.Rank;

				// TODO we only support one-dimensional array for now
				if (rank != 1)
					throw new NotImplementedException("only one-dimensional arrays supported");

				int[] dim = new int[rank];
				int[] lbound = new int[rank];

				// always set 1-based numbering for indexes, we cannot reuse lower and upper bounds from aparam
				for (int i = 0; i < rank; i++)
				{
					lbound[i] = 1;
					dim[i] = aparam.GetLength(i);
				}

				IntPtr a = IntPtr.Zero;
				try
				{
					a = PqsqlWrapper.createPQExpBuffer();

					if (a == IntPtr.Zero)
						throw new OutOfMemoryException("Cannot create PQExpBuffer");

					// create array header
					PqsqlBinaryFormat.pqbf_set_array(a, rank, /* TODO no nulls allowed */ 0, (uint) oid, dim, lbound);

					// copy array items to buffer
					foreach (object o in aparam)
					{
						if (o == null) // null values have itemlength -1 only
						{
							PqsqlBinaryFormat.pqbf_set_array_itemlength(a, -1);
						}
						else
						{
							setArrayItem(a, o);
						}
					}

					// add array to parameter buffer
					PqsqlBinaryFormat.pqbf_add_array(pb, a, (uint) arrayoid);
				}
				finally
				{
					if (a != IntPtr.Zero)
						PqsqlWrapper.destroyPQExpBuffer(a);
				}
			};
		}

	}
}
