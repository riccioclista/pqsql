using System;
using System.Collections.Generic;
using System.Data;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

namespace Pqsql
{
	/// <summary>
	/// Static dictionary of Postgres types, datatype names, GetValue() / SetValue() / SetArrayItem() delegates
	/// </summary>
	internal static class PqsqlTypeRegistry
	{
#region PqsqlTypeRegistry statements
			
			// retrieve type category and name from type oid
			// parameter :o is type oid
			const string TypeCategoryByTypeOid = "select typcategory,typname from pg_type where oid=:o";

#endregion

		internal sealed class PqsqlTypeName
		{
			// used in PqsqlDataReader.GetDataTypeName / PqsqlDataReader.FillSchemaTableColumns
			public string DataTypeName { get; set; }

			// used in PqsqlParameterCollection.CreateParameterBuffer
			public TypeCode TypeCode { get; set; }

			// used in PqsqlDataReader.GetFieldType / PqsqlDataReader.FillSchemaTableColumns
			public Type ProviderType { get; set; }

			// used in PqsqlParameter.PqsqlDbType
			public DbType DbType { get; set; }

			// used in PqsqlTypeRegistry.SetArrayValue
			public PqsqlDbType ArrayDbType { get; set; }

			// used in PqsqlDataReader.GetValue
			public Func<IntPtr, int, int, int, object> GetValue { get; set; }

			// used in PqsqlParameterCollection.AddParameterValue
			public Action<IntPtr, object, PqsqlDbType> SetValue { get; set; }

			// used in PqsqlParameterCollection.AddParameterValue
			public Action<IntPtr, object> SetArrayItem { get; set; }
		}

		// adds o as double array element to PQExpBuffer a
		private static readonly Action<IntPtr, object> setNumericArray = (a, o) =>
		{
			double d = Convert.ToDouble(o);

			long len0 = PqsqlBinaryFormat.pqbf_get_buflen(a); // get start position

			PqsqlBinaryFormat.pqbf_set_array_itemlength(a, -2); // first set an invalid item length
			PqsqlBinaryFormat.pqbf_set_numeric(a, d); // encode numeric value (variable length)

			int len = (int) (PqsqlBinaryFormat.pqbf_get_buflen(a) - len0); // get new buffer length
			// update array item length == len - 4 bytes
			PqsqlBinaryFormat.pqbf_update_array_itemlength(a, -len, len - 4);
		};

		// sets val as string with Oid oid (PqsqlDbType.BPChar, PqsqlDbType.Text, PqsqlDbType.Varchar, PqsqlDbType.Name, PqsqlDbType.Char)
		// into pqparam_buffer pb
		private static readonly Action<IntPtr, object, PqsqlDbType> setText = (pb, val, oid) =>
		{
			unsafe
			{
				fixed (char* t = (string) val)
				{
					PqsqlBinaryFormat.pqbf_add_unicode_text(pb, t, (uint) oid);
				}
			}
		};

		// adds o as string array element to PQExpBuffer a
		private static readonly Action<IntPtr, object> setTextArray = (a, o) =>
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

		// sets val as DateTime with Oid oid (PqsqlDbType.Timestamp, PqsqlDbType.TimestampTZ) into pqparam_buffer pb
		private static readonly Action<IntPtr, object, PqsqlDbType> setTimestamp = (pb, val, oid) =>
		{
			DateTime dt = (DateTime) val;

            // we always interpret dt as Utc timestamp and ignore DateTime.Kind value
			long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
			long sec = ticks / TimeSpan.TicksPerSecond;
			int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);
			PqsqlBinaryFormat.pqbf_add_timestamp(pb, sec, usec, (uint) oid);
		};

		// adds o as DateTime array element into PQExpBuffer a
		private static readonly Action<IntPtr, object> setTimestampArray = (a, o) =>
		{
			DateTime dt = (DateTime) o;

            // we always interpret dt as Utc timestamp and ignore DateTime.Kind value
			long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
			long sec = ticks / TimeSpan.TicksPerSecond;
			int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);
			PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
			PqsqlBinaryFormat.pqbf_set_timestamp(a, sec, usec);
		};

		// maps PqsqlDbType to PqsqlTypeName
		static readonly Dictionary<PqsqlDbType, PqsqlTypeName> mPqsqlDbTypeDict = new Dictionary<PqsqlDbType, PqsqlTypeName>
    {
			{ PqsqlDbType.Boolean,
				new PqsqlTypeName { 
					DataTypeName="bool",
					TypeCode=TypeCode.Boolean,
					ProviderType=typeof(bool),
					DbType=DbType.Boolean,
					ArrayDbType=PqsqlDbType.BooleanArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetBoolean(res,row,ord),
					SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_bool(pb, (bool) val ? 1 : 0),
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
					DataTypeName="float8",
					TypeCode=TypeCode.Double,
					ProviderType=typeof(double),
					DbType=DbType.Double,
					ArrayDbType=PqsqlDbType.Float8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDouble(res,row,ord),
					SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_float8(pb, (double) val),
					SetArrayItem = (a, o) => {
						double v = (double) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
						PqsqlBinaryFormat.pqbf_set_float8(a, v);
					}
				}
			},
			{ PqsqlDbType.Int4,
				new PqsqlTypeName {
					DataTypeName="int4",
					TypeCode=TypeCode.Int32,
					ProviderType=typeof(int),
					DbType=DbType.Int32,
					ArrayDbType=PqsqlDbType.Int4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt32(res,row,ord),
					SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_int4(pb, (int) val),
					SetArrayItem = (a, o) => {
						int v = (int) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 4);
						PqsqlBinaryFormat.pqbf_set_int4(a, v);
					}
				}
			},
			{ PqsqlDbType.Int8,
				new PqsqlTypeName {
					DataTypeName="int8",
					TypeCode=TypeCode.Int64,
					ProviderType=typeof(long),
					DbType=DbType.Int64,
					ArrayDbType=PqsqlDbType.Int8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt64(res,row,ord),
					SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_int8(pb, (long) val),
					SetArrayItem = (a, o) => {
						long v = (long) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
						PqsqlBinaryFormat.pqbf_set_int8(a, v);
					}
				}
			},
			{ PqsqlDbType.Numeric,
				new PqsqlTypeName {
					DataTypeName="numeric",
					TypeCode=TypeCode.Decimal,
					ProviderType=typeof(Decimal),
					DbType=DbType.VarNumeric,
					ArrayDbType=PqsqlDbType.NumericArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetNumeric(res,row,ord,typmod),
					SetValue=(pb, val, oid) => {
						double d = Convert.ToDouble(val);
						PqsqlBinaryFormat.pqbf_add_numeric(pb, d);
					},
					SetArrayItem = setNumericArray
				}
			},
			{ PqsqlDbType.Float4,
				new PqsqlTypeName {
					DataTypeName="float4",
					TypeCode=TypeCode.Single,
					ProviderType=typeof(float),
					DbType=DbType.Single,
					ArrayDbType=PqsqlDbType.Float4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetFloat(res,row,ord),
					SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_float4(pb, (float) val),
					SetArrayItem = (a, o) => {
						float v = (float) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 4);
						PqsqlBinaryFormat.pqbf_set_float4(a, v);
					}
				}
			},
			{ PqsqlDbType.Int2,
				new PqsqlTypeName {
					DataTypeName="int2",
					TypeCode=TypeCode.Int16,
					ProviderType=typeof(short),
					DbType=DbType.Int16,
					ArrayDbType=PqsqlDbType.Int2Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt16(res,row,ord),
					SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_int2(pb, (short) val),
					SetArrayItem = (a, o) => {
						short v = (short) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 2);
						PqsqlBinaryFormat.pqbf_set_int2(a, v);
					}
				}
			},
			{ PqsqlDbType.BPChar,
				new PqsqlTypeName {
					DataTypeName="bpchar",
					TypeCode=TypeCode.String,
					ProviderType=typeof(string),
					DbType=DbType.StringFixedLength,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue=setText,
					SetArrayItem=setTextArray
				}
			},
			{ PqsqlDbType.Text,
				new PqsqlTypeName {
					DataTypeName="text",
					TypeCode=TypeCode.String,
					ProviderType=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.TextArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.Varchar,
				new PqsqlTypeName {
					DataTypeName="varchar",
					TypeCode=TypeCode.String,
					ProviderType=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.VarcharArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.Name,
				new PqsqlTypeName {
					DataTypeName="name",
					TypeCode=TypeCode.String,
					ProviderType=typeof(string),
					DbType=DbType.StringFixedLength,
					ArrayDbType=PqsqlDbType.NameArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.Char,
				new PqsqlTypeName {
					DataTypeName="char",
					TypeCode=TypeCode.SByte,
					ProviderType=typeof(sbyte),
					DbType=DbType.SByte,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetByte(res,row,ord),
					SetValue=null, // TODO
					SetArrayItem = null // TODO
				}
			},
			{ PqsqlDbType.Bytea,
				new PqsqlTypeName {
					DataTypeName="bytea",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(byte[]),
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
					SetValue= (pb, val, oid) =>
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
					DataTypeName="date",
					TypeCode=TypeCode.DateTime,
					ProviderType=typeof(DateTime),
					DbType=DbType.Date,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => new DateTime(PqsqlDataReader.GetDate(res,row,ord)),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_date(pb, (DateTime) val); }
					SetArrayItem = null // TODO
				}
			},
			{ PqsqlDbType.Time,
				new PqsqlTypeName {
					DataTypeName="time",
					TypeCode=TypeCode.DateTime,
					ProviderType=typeof(DateTime),
					DbType=DbType.Time,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => new DateTime(PqsqlDataReader.GetTime(res,row,ord)),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_time(pb, (DateTime) val); }
					SetArrayItem = null // TODO
				}
			},
			{ PqsqlDbType.Timestamp,
				new PqsqlTypeName {
					DataTypeName="timestamp",
					TypeCode=TypeCode.DateTime,
					ProviderType=typeof(DateTime),
					DbType=DbType.DateTime,
					ArrayDbType=PqsqlDbType.TimestampArray,
					GetValue=(res, row, ord, typmod) => new DateTime(PqsqlDataReader.GetDateTime(res,row,ord)),
					SetValue = setTimestamp,
					SetArrayItem = setTimestampArray
				}
			},
			{ PqsqlDbType.TimestampTZ,
				new PqsqlTypeName {
					DataTypeName="timestamptz",
					TypeCode=TypeCode.DateTime,
					ProviderType=typeof(DateTime),
					DbType=DbType.DateTimeOffset,
					ArrayDbType=PqsqlDbType.TimestampTZArray,
					GetValue=(res, row, ord, typmod) => new DateTimeOffset(PqsqlDataReader.GetDateTime(res,row,ord), TimeSpan.Zero),
					SetValue = setTimestamp,
					SetArrayItem = setTimestampArray
				}
			},
			{ PqsqlDbType.Interval,
				new PqsqlTypeName {
					DataTypeName="interval",
					TypeCode=TypeCode.DateTime,
					ProviderType=typeof(TimeSpan),
					DbType=DbType.DateTime,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInterval(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_interval(pb, (DateTime) val); }
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.TimeTZ,
				new PqsqlTypeName {
					DataTypeName="timetz",
					TypeCode=TypeCode.DateTime,
					ProviderType=typeof(DateTime),
					DbType=DbType.DateTime,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetTime(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_timetz(pb, (DateTime) val); }
					SetArrayItem = null
				}
			},
			//{ PqsqlDbType.Inet, new PqsqlTypeName { Name="inet", ProviderType=typeof() } },
			//{ PqsqlDbType.Cidr, new PqsqlTypeName { Name="cidr", ProviderType=typeof() } },
			//{ PqsqlDbType.MacAddr, new PqsqlTypeName { Name="macaddr", ProviderType=typeof() } },
			//{ PqsqlDbType.Bit, new PqsqlTypeName { Name="bit", ProviderType=typeof() } },
			//{ PqsqlDbType.Varbit, new PqsqlTypeName { Name="varbit", ProviderType=typeof() } },
			{ PqsqlDbType.Uuid,
				new PqsqlTypeName {
					DataTypeName="uuid",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Guid),
					DbType=DbType.Guid,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetGuid(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_uuid(pb, (Guid) val); }
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Refcursor,
				new PqsqlTypeName {
					DataTypeName="refcursor",
					TypeCode=TypeCode.String,
					ProviderType=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.Oid,
				new PqsqlTypeName {
					DataTypeName="oid",
					TypeCode=TypeCode.UInt32,
					ProviderType=typeof(uint),
					DbType=DbType.UInt32,
					ArrayDbType=PqsqlDbType.OidArray,
					GetValue=(res, row, ord, typmod) => (uint) PqsqlDataReader.GetInt32(res,row,ord),
					SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_oid(pb, (uint) val),
					SetArrayItem = (a, o) => {
						uint v = (uint) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 4);
						PqsqlBinaryFormat.pqbf_set_oid(a, v);
					}
				}
			},
			{ PqsqlDbType.Unknown,
				new PqsqlTypeName {
					DataTypeName="unknown",
					TypeCode=TypeCode.String,
					ProviderType=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue = setText,
					SetArrayItem = null
				}
			},


			{ PqsqlDbType.BooleanArray,
				new PqsqlTypeName {
					DataTypeName="_bool",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.BooleanArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Boolean, typeof(bool?), typeof(bool), (x, len) => PqsqlBinaryFormat.pqbf_get_bool(x) > 0),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Int2Array,
				new PqsqlTypeName {
					DataTypeName="_int2",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Int2Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Int2, typeof(short?), typeof(short), (x, len) => PqsqlBinaryFormat.pqbf_get_int2(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Int4Array,
				new PqsqlTypeName {
					DataTypeName="_int4",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Int4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Int4, typeof(int?), typeof(int), (x, len) => PqsqlBinaryFormat.pqbf_get_int4(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},

			{ PqsqlDbType.TextArray,
				new PqsqlTypeName {
					DataTypeName="_text",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.TextArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Text, typeof(string), typeof(string), PqsqlDataReader.GetStringValue),
					SetValue = null,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.NameArray,
				new PqsqlTypeName {
					DataTypeName="_name",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.NameArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Name, typeof(string), typeof(string), PqsqlDataReader.GetStringValue),
					SetValue = null,
					SetArrayItem = setTextArray
				}
			},
			{ PqsqlDbType.VarcharArray,
				new PqsqlTypeName {
					DataTypeName="_varchar",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.VarcharArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Varchar, typeof(string), typeof(string), PqsqlDataReader.GetStringValue),
					SetValue = null,
					SetArrayItem = setTextArray
				}
			},

			{ PqsqlDbType.Int8Array,
				new PqsqlTypeName {
					DataTypeName="_int8",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Int8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Int8, typeof(long?), typeof(long), (x, len) => PqsqlBinaryFormat.pqbf_get_int8(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Float4Array,
				new PqsqlTypeName {
					DataTypeName="_float4",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Float4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Float4, typeof(float?), typeof(float), (x, len) => PqsqlBinaryFormat.pqbf_get_float4(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Float8Array,
				new PqsqlTypeName {
					DataTypeName="_float8",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Float8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Float8, typeof(double?), typeof(double), (x, len) => PqsqlBinaryFormat.pqbf_get_float8(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.OidArray,
				new PqsqlTypeName {
					DataTypeName="_oid",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.OidArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Oid, typeof(uint?), typeof(uint), (x, len) => (uint) PqsqlBinaryFormat.pqbf_get_int4(x)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.TimestampArray,
				new PqsqlTypeName {
					DataTypeName="_timestamp",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
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
					DataTypeName="_timestamptz",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.TimestampTZArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.TimestampTZ, typeof(DateTimeOffset?), typeof(DateTimeOffset), (x, len) => {
						long sec;
						int usec;
						unsafe { PqsqlBinaryFormat.pqbf_get_timestamp(x, &sec, &usec); }
						long ticks = PqsqlBinaryFormat.UnixEpochTicks + sec * TimeSpan.TicksPerSecond + usec * 10;
						DateTimeOffset dt = new DateTimeOffset(ticks, TimeSpan.Zero);
						return dt;
					}),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.NumericArray,
				new PqsqlTypeName {
					DataTypeName="_numeric",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.NumericArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Numeric, typeof(double?), typeof(double), (x, len) => PqsqlBinaryFormat.pqbf_get_numeric(x,typmod)),
					SetValue=null,
					SetArrayItem = null
				}
			},
			{ PqsqlDbType.Void,
				new PqsqlTypeName { 
					DataTypeName="void",
					TypeCode=TypeCode.Object,
					ProviderType=typeof(object),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue= (pb, val, oid) => { throw new  InvalidOperationException("Cannot set void parameter"); },
					SetArrayItem = (a, o) => { throw new InvalidOperationException("Cannot set void array parameter"); }
				}
			},
    };

		// maps DbType to PqsqlDbType
		static readonly PqsqlDbType[] mDbTypeArray = {
			// DbType.AnsiString = 0,
			PqsqlDbType.Text,

			// DbType.Binary = 1,
			PqsqlDbType.Unknown,

			// DbType.Byte = 2,
			PqsqlDbType.Int2,

			// DbType.Boolean = 3,
			PqsqlDbType.Boolean,

			// DbType.Currency = 4,
			PqsqlDbType.Cash,
			
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

		// used in PqsqlDataReader.PopulateRowInfoAndOutputParameters and PqsqlParameterCollection.CreateParameterBuffer
		internal static PqsqlTypeName Get(PqsqlDbType oid)
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<PqsqlTypeName>() == null || Contract.Result<PqsqlTypeName>().GetValue != null);
			Contract.Ensures(Contract.Result<PqsqlTypeName>() == null || Contract.Result<PqsqlTypeName>().DataTypeName != null);
			Contract.Ensures(Contract.Result<PqsqlTypeName>() == null || Contract.Result<PqsqlTypeName>().ProviderType != null);
#endif

			PqsqlTypeName result;
			return mPqsqlDbTypeDict.TryGetValue(oid, out result) ? result : null;
		}

		// used in PqsqlDataReader.PopulateRowInfoAndOutputParameters
		internal static PqsqlTypeName FetchType(PqsqlDbType oid, string connstring)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentOutOfRangeException>(oid != 0, "Datatype with oid=0 (InvalidOid) not supported");
			Contract.Requires<ArgumentNullException>(connstring != null);

			Contract.Ensures(Contract.Result<PqsqlTypeName>() != null);
			Contract.Ensures(Contract.Result<PqsqlTypeName>().GetValue != null);
			Contract.Ensures(Contract.Result<PqsqlTypeName>().DataTypeName != null);
			Contract.Ensures(Contract.Result<PqsqlTypeName>().ProviderType != null);
#else
			if (oid == 0)
				throw new ArgumentOutOfRangeException("Datatype with oid=0 (InvalidOid) not supported");
			if (connstring == null)
				throw new ArgumentNullException("connstring");
#endif

			// try to guess the type mapping
			// we must open a new connection here, since we have already a running query when we call FetchType
			// TODO when we have query pipelining, we might not need to open fresh connections here https://commitfest.postgresql.org/10/634/ http://2ndquadrant.github.io/postgres/libpq-batch-mode.html 
			using (PqsqlConnection conn = new PqsqlConnection(connstring))
			using (PqsqlCommand cmd = new PqsqlCommand(TypeCategoryByTypeOid, conn))
			{
				PqsqlParameter p_oid = new PqsqlParameter
				{
					ParameterName = "o",
					PqsqlDbType = PqsqlDbType.Oid,
					Value = oid
				};

				cmd.Parameters.Add(p_oid);

				using (PqsqlDataReader reader = cmd.ExecuteReader())
				{
					if (reader.Read())
					{
						byte typcategory = reader.GetByte(0);

						// see http://www.postgresql.org/docs/current/static/catalog-pg-type.html#CATALOG-TYPCATEGORY-TABLE
						if (typcategory == 'S')
						{
							string typname = reader.GetString(1);

							if (typname == null)
								throw new PqsqlException("Could not fetch datatype name " + oid);

							// assume that we can use this type just like PqsqlDbType.Text (e.g., citext)
							PqsqlTypeName tn = new PqsqlTypeName
							{
								DataTypeName = typname,
								TypeCode = TypeCode.String,
								ProviderType = typeof (string),
								DbType = DbType.String,
								ArrayDbType = PqsqlDbType.Array,
								GetValue = (res, row, ord, typmod) => PqsqlDataReader.GetString(res, row, ord),
								SetValue = setText,
								SetArrayItem = setTextArray
							};

							// TODO cache maintainance not implemented here! what about different databases?
							mPqsqlDbTypeDict.Add(oid, tn);

							return tn;
						}
					}
				}
			}

			throw new NotSupportedException("Datatype " + oid + " not supported");
		}

		// used in PqsqlParameter.DbType
		public static PqsqlDbType GetPqsqlDbType(DbType dbType)
		{
#if CODECONTRACTS
			Contract.Assume((int) dbType < mDbTypeArray.Length);
#endif
			return mDbTypeArray[(int) dbType];
		}

		// used in PqsqlParameter.PqsqlDbType
		public static DbType GetDbType(PqsqlDbType oid)
		{
			PqsqlTypeName tn = Get(oid);

			if (tn == null)
			{
				// do not try to fetch datatype specs with PqsqlTypeRegistry.FetchType() here, just bail out
				throw new NotSupportedException(string.Format("Datatype {0} is not supported", oid & ~PqsqlDbType.Array));
			}

			return tn.DbType;
		}

		// used in PqsqlParameterCollection.AddParameterValue
		public static Action<IntPtr, object> SetArrayValue(PqsqlDbType oid, PqsqlTypeName n)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(n != null);
#else
			if (n == null)
				throw new ArgumentNullException("n");
#endif

			oid &= ~PqsqlDbType.Array; // remove Array flag
			PqsqlDbType arrayoid = n.ArrayDbType;
			Action<IntPtr, object> setArrayItem = n.SetArrayItem;

			// return closure
			return (pb, val) =>
			{
				Array aparam = val as Array;
				int rank = aparam.Rank;
				int hasNulls = 0;

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

					foreach (object o in aparam)
					{
						if (o == null || o == DBNull.Value)
						{
							hasNulls = 1;
							break;
						}
					}

					// create array header
					PqsqlBinaryFormat.pqbf_set_array(a, rank, hasNulls, (uint) oid, dim, lbound);

					// copy array items to buffer
					foreach (object o in aparam)
					{
						if (o == null || o == DBNull.Value) // null values have itemlength -1 only
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
