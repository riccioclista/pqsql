using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

using PqsqlBinaryFormat = Pqsql.UnsafeNativeMethods.PqsqlBinaryFormat;

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

		internal static PqsqlDbType DateTimeOffsetOid;

		/// <summary>
		/// stores Type, datatype name, and GetValue delegate for PqsqlDataReader
		/// </summary>
		internal sealed class PqsqlTypeValue
		{
			// used in PqsqlDataReader.GetDataTypeName / PqsqlDataReader.FillSchemaTableColumns
			public string DataTypeName { get; set; }

			// used in PqsqlDataReader.GetFieldType / PqsqlDataReader.FillSchemaTableColumns
			public Type ProviderType { get; set; }

			// used in PqsqlDataReader.GetValue / PqsqlDataReader.PopulateRowInfoAndOutputParameters
			public Func<IntPtr, int, int, int, object> GetValue { get; set; }
		}

		/// <summary>
		/// stores TypeCode, PqsqlDbType for arrays, SetValue, and SetArrayItem delegates for PqsqlParameterBuffer
		/// </summary>
		internal sealed class PqsqlTypeParameter
		{
			// used in PqsqlParameterBuffer.AddParameter
			public TypeCode TypeCode { get; set; }

			// used in PqsqlTypeRegistry.SetArrayValue
			public PqsqlDbType ArrayDbType { get; set; }

			// used in PqsqlParameterBuffer.AddParameterValue
			public Action<IntPtr, object, PqsqlDbType> SetValue { get; set; }

			// used in PqsqlParameterBuffer.AddParameterValue
			public Action<IntPtr, object> SetArrayItem { get; set; }
		}

		/// <summary>
		/// entry for static PqsqlDbType dictionary: TypeValue, TypeParameter, and DbType
		/// </summary>
		private sealed class PqsqlTypeEntry
		{
			public PqsqlTypeValue TypeValue { get; set; }

			public PqsqlTypeParameter TypeParameter { get; set; }

			// used in PqsqlParameter.PqsqlDbType
			public DbType DbType { get; set; }
		}

		// maps PqsqlDbType to PqsqlTypeEntry
		private static readonly Dictionary<PqsqlDbType, PqsqlTypeEntry> mPqsqlDbTypeDict = new Dictionary<PqsqlDbType, PqsqlTypeEntry>
		{
			{ PqsqlDbType.Boolean,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="bool",
						ProviderType=typeof(bool),
						GetValue =(res, row, ord, typmod) => PqsqlDataReader.GetBoolean(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Boolean,
						ArrayDbType=PqsqlDbType.BooleanArray,
						SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_bool(pb, (bool) val ? 1 : 0),
						SetArrayItem = (a, o) => PqsqlParameterBuffer.SetTypeArray(a, sizeof(bool), PqsqlBinaryFormat.pqbf_set_bool, (bool) o ? 1 : 0),
					},
					DbType=DbType.Boolean,
				}
			},
			{ PqsqlDbType.Float8,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="float8",
						ProviderType=typeof(double),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDouble(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode =TypeCode.Double,
						ArrayDbType=PqsqlDbType.Float8Array,
						SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_float8(pb, (double) val),
						SetArrayItem = (a, o) => PqsqlParameterBuffer.SetTypeArray(a, sizeof(double), PqsqlBinaryFormat.pqbf_set_float8, (double) o),
					},
					DbType=DbType.Double,
				}
			},
			{ PqsqlDbType.Int4,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="int4",
						ProviderType=typeof(int),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt32(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Int32,
						ArrayDbType=PqsqlDbType.Int4Array,
						SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_int4(pb, (int) val),
						SetArrayItem = (a, o) => PqsqlParameterBuffer.SetTypeArray(a, sizeof(int), PqsqlBinaryFormat.pqbf_set_int4, (int) o),
					},
					DbType=DbType.Int32,
				}
			},
			{ PqsqlDbType.Int8,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="int8",
						ProviderType=typeof(long),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt64(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Int64,
						ArrayDbType=PqsqlDbType.Int8Array,
						SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_int8(pb, (long) val),
						SetArrayItem = (a, o) => PqsqlParameterBuffer.SetTypeArray(a, sizeof(long), PqsqlBinaryFormat.pqbf_set_int8, (long) o),
					},
					DbType=DbType.Int64,
				}
			},
			{ PqsqlDbType.Numeric,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="numeric",
						ProviderType=typeof(Decimal),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetNumeric(res,row,ord,typmod),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Decimal,
						ArrayDbType=PqsqlDbType.NumericArray,
						SetValue=(pb, val, oid) => {
							double d = Convert.ToDouble(val, CultureInfo.InvariantCulture);
							PqsqlBinaryFormat.pqbf_add_numeric(pb, d);
						},
						SetArrayItem = PqsqlParameterBuffer.SetNumericArray
					},
					DbType=DbType.VarNumeric,
				}
			},
			{ PqsqlDbType.Float4,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="float4",
						ProviderType=typeof(float),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetFloat(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Single,
						ArrayDbType=PqsqlDbType.Float4Array,
						SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_float4(pb, (float) val),
						SetArrayItem = (a, o) => PqsqlParameterBuffer.SetTypeArray(a, sizeof(float), PqsqlBinaryFormat.pqbf_set_float4, (float) o),
					},
					DbType=DbType.Single,
				}
			},
			{ PqsqlDbType.Int2,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="int2",
						ProviderType=typeof(short),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt16(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Int16,
						ArrayDbType=PqsqlDbType.Int2Array,
						SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_int2(pb, (short) val),
						SetArrayItem = (a, o) => PqsqlParameterBuffer.SetTypeArray(a, sizeof(short), PqsqlBinaryFormat.pqbf_set_int2, (short) o),
					},
					DbType =DbType.Int16,
				}
			},
			{ PqsqlDbType.BPChar,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="bpchar",
						ProviderType=typeof(string),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.String,
						ArrayDbType=PqsqlDbType.BPCharArray,
						SetValue= PqsqlParameterBuffer.SetText,
						SetArrayItem= PqsqlParameterBuffer.SetTextArray
					},
					DbType=DbType.StringFixedLength,
				}
			},
			{ PqsqlDbType.Text,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="text",
						ProviderType=typeof(string),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.String,
						ArrayDbType=PqsqlDbType.TextArray,
						SetValue = PqsqlParameterBuffer.SetText,
						SetArrayItem= PqsqlParameterBuffer.SetTextArray
					},
					DbType=DbType.String,
				}
			},
			{ PqsqlDbType.Varchar,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="varchar",
						ProviderType=typeof(string),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.String,
						ArrayDbType=PqsqlDbType.VarcharArray,
						SetValue = PqsqlParameterBuffer.SetText,
						SetArrayItem= PqsqlParameterBuffer.SetTextArray
					},
					DbType=DbType.String,
				}
			},
			{ PqsqlDbType.Name,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName ="name",
						ProviderType=typeof(string),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.String,
						ArrayDbType=PqsqlDbType.NameArray,
						SetValue = PqsqlParameterBuffer.SetText,
						SetArrayItem= PqsqlParameterBuffer.SetTextArray
					},				
					DbType=DbType.StringFixedLength,
				}
			},
			{ PqsqlDbType.Char,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="char",
						ProviderType=typeof(sbyte),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetSByte(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.SByte,
						ArrayDbType=PqsqlDbType.CharArray,
						SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_char(pb, (sbyte) val),
						SetArrayItem = (a, c) => {
							PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 1);
							PqsqlBinaryFormat.pqbf_set_char(a, (sbyte) c);
						},
					},
					DbType=DbType.SByte,
				}
			},
			{ PqsqlDbType.Bytea,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="bytea",
						ProviderType=typeof(byte[]),
						GetValue= (res, row, ord, typmod) => {
							int n = (int) PqsqlDataReader.GetBytes(res, row, ord, 0, null, 0, 0);
							byte[] bs = new byte[n];
							n = (int) PqsqlDataReader.GetBytes(res, row, ord, 0, bs, 0, n);

							if (n != bs.Length)
								throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Received wrong number of bytes ({0}) for byte array ({1})", n, bs.Length));

							return bs;
						},
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue= (pb, val, oid) => {
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
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Date,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="date",
						ProviderType=typeof(DateTime),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDate(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.DateTime,
						ArrayDbType=PqsqlDbType.DateArray,
						SetValue = PqsqlParameterBuffer.SetDate,
						SetArrayItem = PqsqlParameterBuffer.SetDateArray
					},
					DbType=DbType.Date,
				}
			},
			{ PqsqlDbType.Time,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="time",
						ProviderType=typeof(DateTime),
						GetValue=(res, row, ord, typmod) => new TimeSpan(PqsqlDataReader.GetTime(res,row,ord)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.TimeArray,
						SetValue = PqsqlParameterBuffer.SetTime,
						SetArrayItem = PqsqlParameterBuffer.SetTimeArray
					},
					DbType=DbType.Time,
				}
			},
			{ PqsqlDbType.Timestamp,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="timestamp",
						ProviderType=typeof(DateTime),
						GetValue=(res, row, ord, typmod) => new DateTime(PqsqlDataReader.GetDateTime(res,row,ord)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.DateTime,
						ArrayDbType=PqsqlDbType.TimestampArray,
						SetValue = PqsqlParameterBuffer.SetTimestamp,
						SetArrayItem = PqsqlParameterBuffer.SetTimestampArray
					},
					DbType=DbType.DateTime,
				}
			},
			{ PqsqlDbType.TimestampTZ,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="timestamptz",
						ProviderType=typeof(DateTime),
						GetValue=(res, row, ord, typmod) => new DateTimeOffset(PqsqlDataReader.GetDateTime(res,row,ord), TimeSpan.Zero),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.DateTime,
						ArrayDbType=PqsqlDbType.TimestampTZArray,
						SetValue = PqsqlParameterBuffer.SetTimestamp,
						SetArrayItem = PqsqlParameterBuffer.SetTimestampArray
					},
					DbType=DbType.DateTimeOffset,
				}
			},
			{ PqsqlDbType.Interval,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="interval",
						ProviderType=typeof(TimeSpan),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInterval(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.IntervalArray,
						SetValue=PqsqlParameterBuffer.SetInterval,
						SetArrayItem = PqsqlParameterBuffer.SetIntervalArray,
					},
					DbType=DbType.DateTime,
				}
			},
			{ PqsqlDbType.TimeTZ,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="timetz",
						ProviderType=typeof(TimeSpan),
						GetValue=(res, row, ord, typmod) => {
							long ticks = PqsqlDataReader.GetTimeTZ(res, row, ord);
							return new TimeSpan(ticks);
						},
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.TimeTZArray,
						SetValue=PqsqlParameterBuffer.SetTimeTZ,
						SetArrayItem = PqsqlParameterBuffer.SetTimeTZArray,
					},
					DbType=DbType.DateTime,
				}
			},
			{ PqsqlDbType.Inet,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="inet",
						ProviderType=typeof(IPAddress),
						GetValue=null, // TODO
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue=null, // TODO
						SetArrayItem = null // TODO
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Cidr,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="cidr",
						ProviderType=typeof(IPAddress),
						GetValue=null, // TODO
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue=null, // TODO
						SetArrayItem = null // TODO
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.MacAddr,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="macaddr",
						ProviderType=typeof(PhysicalAddress),
						GetValue=null, // TODO
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue=null, // TODO
						SetArrayItem = null // TODO
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Bit,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="bit",
						ProviderType=typeof(BitArray),
						GetValue=null, // TODO
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue=null, // TODO
						SetArrayItem = null // TODO
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Varbit,new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="varbit",
						ProviderType=typeof(BitArray),
						GetValue=null, // TODO
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue=null, // TODO
						SetArrayItem = null // TODO
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Uuid,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="uuid",
						ProviderType=typeof(Guid),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetGuid(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_uuid(pb, (Guid) val); }
						SetArrayItem = null // TODO
					},
					DbType=DbType.Guid,
				}
			},
			{ PqsqlDbType.Refcursor,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="refcursor",
						ProviderType=typeof(string),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.String,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue = PqsqlParameterBuffer.SetText,
						SetArrayItem = PqsqlParameterBuffer.SetTextArray
					},
					DbType=DbType.String,
				}
			},
			{ PqsqlDbType.Oid,
				new PqsqlTypeEntry {
					TypeValue=new PqsqlTypeValue {
						DataTypeName="oid",
						ProviderType=typeof(uint),
						GetValue=(res, row, ord, typmod) => (uint) PqsqlDataReader.GetInt32(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.UInt32,
						ArrayDbType=PqsqlDbType.OidArray,
						SetValue=(pb, val, oid) => PqsqlBinaryFormat.pqbf_add_oid(pb, (uint) val),
						SetArrayItem = (a, o) => PqsqlParameterBuffer.SetTypeArray(a, sizeof(uint), PqsqlBinaryFormat.pqbf_set_oid, (uint) o),
					},
					DbType=DbType.UInt32,
				}
			},
			{ PqsqlDbType.Unknown,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="unknown",
						ProviderType=typeof(string),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.String,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue = PqsqlParameterBuffer.SetText,
						SetArrayItem = null // TODO
					},
					DbType=DbType.String,
				}
			},
			{ PqsqlDbType.BooleanArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_bool",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Boolean, typeof(bool?), typeof(bool), (x, len) => PqsqlBinaryFormat.pqbf_get_bool(x) > 0),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.BooleanArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.CharArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_char",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Char, typeof(sbyte?), typeof(sbyte), (x, len) => PqsqlBinaryFormat.pqbf_get_char(x)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.CharArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Int2Array,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_int2",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Int2, typeof(short?), typeof(short), (x, len) => PqsqlBinaryFormat.pqbf_get_int2(x)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Int2Array,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Int4Array,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_int4",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Int4, typeof(int?), typeof(int), (x, len) => PqsqlBinaryFormat.pqbf_get_int4(x)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Int4Array,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.TextArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_text",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Text, typeof(string), typeof(string), PqsqlDataReader.GetStringValue),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.TextArray,
						SetValue=null,
						SetArrayItem = PqsqlParameterBuffer.SetTextArray
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.NameArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_name",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Name, typeof(string), typeof(string), PqsqlDataReader.GetStringValue),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.NameArray,
						SetValue = null,
						SetArrayItem = PqsqlParameterBuffer.SetTextArray
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.VarcharArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_varchar",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Varchar, typeof(string), typeof(string), PqsqlDataReader.GetStringValue),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.VarcharArray,
						SetValue = null,
						SetArrayItem = PqsqlParameterBuffer.SetTextArray
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Int8Array,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_int8",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Int8, typeof(long?), typeof(long), (x, len) => PqsqlBinaryFormat.pqbf_get_int8(x)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Int8Array,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Float4Array,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_float4",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Float4, typeof(float?), typeof(float), (x, len) => PqsqlBinaryFormat.pqbf_get_float4(x)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Float4Array,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Float8Array,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_float8",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Float8, typeof(double?), typeof(double), (x, len) => PqsqlBinaryFormat.pqbf_get_float8(x)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Float8Array,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.OidArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_oid",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Oid, typeof(uint?), typeof(uint), (x, len) => (uint) PqsqlBinaryFormat.pqbf_get_int4(x)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.OidArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.TimestampArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_timestamp",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Timestamp, typeof(DateTime?), typeof(DateTime), (x, len) => {
							long sec;
							int usec;
							unsafe { PqsqlBinaryFormat.pqbf_get_timestamp(x, &sec, &usec); }
							long ticks = PqsqlBinaryFormat.GetTicksFromTimestamp(sec, usec);
							DateTime dt = new DateTime(ticks);
							return dt;
						}),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.TimestampArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.TimestampTZArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_timestamptz",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.TimestampTZ, typeof(DateTimeOffset?), typeof(DateTimeOffset), (x, len) => {
							long sec;
							int usec;
							unsafe { PqsqlBinaryFormat.pqbf_get_timestamp(x, &sec, &usec); }
							long ticks = PqsqlBinaryFormat.GetTicksFromTimestamp(sec, usec);
							DateTimeOffset dt = new DateTimeOffset(ticks, TimeSpan.Zero);
							return dt;
						}),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.TimestampTZArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.TimeArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_time",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Time, typeof(TimeSpan?), typeof(TimeSpan), (x, len) => {
							int hour;
							int min;
							int sec;
							int fsec;
							unsafe { PqsqlBinaryFormat.pqbf_get_time(x, &hour, &min, &sec, &fsec); }
							long ticks = PqsqlBinaryFormat.GetTicksFromTime(hour, min, sec, fsec);
							TimeSpan ts = new TimeSpan(ticks);
							return ts;
						}),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.TimeArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.TimeTZArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_timetz",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.TimeTZ, typeof(TimeSpan?), typeof(TimeSpan), (x, len) => {
							int hour;
							int min;
							int sec;
							int fsec;
							int tz;
							unsafe { PqsqlBinaryFormat.pqbf_get_timetz(x, &hour, &min, &sec, &fsec, &tz); }
							long ticks = PqsqlBinaryFormat.GetTicksFromTimeTZ(hour, min, sec, fsec, tz);
							TimeSpan ts = new TimeSpan(ticks);
							return ts;
						}),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.TimeArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.DateArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_date",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Date, typeof(DateTime?), typeof(DateTime), (x, len) => {
							int year;
							int month;
							int day;
							unsafe { PqsqlBinaryFormat.pqbf_get_date(x, &year, &month, &day); }
							return PqsqlBinaryFormat.GetDateTimeFromDate(year, month, day);
						}),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.DateArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.IntervalArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_interval",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Interval, typeof(TimeSpan?), typeof(TimeSpan), (x, len) => {
							long offset;
							int day;
							int month;
							unsafe { PqsqlBinaryFormat.pqbf_get_interval(x, &offset, &day, &month); }
							return PqsqlBinaryFormat.GetTimeSpan(offset, day, month);
						}),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.IntervalArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.NumericArray,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="_numeric",
						ProviderType=typeof(Array),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetArrayFill(res, row, ord, PqsqlDbType.Numeric, typeof(double?), typeof(double), (x, len) => PqsqlBinaryFormat.pqbf_get_numeric(x,typmod)),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.NumericArray,
						SetValue=null,
						SetArrayItem = null
					},
					DbType=DbType.Object,
				}
			},
			{ PqsqlDbType.Void,
				new PqsqlTypeEntry {
					TypeValue =new PqsqlTypeValue {
						DataTypeName="void",
						ProviderType=typeof(object),
						GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					},
					TypeParameter = new PqsqlTypeParameter {
						TypeCode=TypeCode.Object,
						ArrayDbType=PqsqlDbType.Array, // TODO
						SetValue= (pb, val, oid) => { throw new  InvalidOperationException("Cannot set void parameter"); },
						SetArrayItem = (a, o) => { throw new InvalidOperationException("Cannot set void array parameter"); }
					},
					DbType=DbType.Object,
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

		// maps connection strings to user-defined datatypes
		private static readonly ConcurrentDictionary<string, Dictionary<PqsqlDbType, PqsqlTypeEntry>> mUserTypesDict  = new ConcurrentDictionary<string, Dictionary<PqsqlDbType, PqsqlTypeEntry>>();


		#region access types for PqsqlParameterBuffer

		// used in PqsqlParameterBuffer.AddParameter
		// TODO user-defined datatypes not supported
		internal static PqsqlTypeParameter Get(PqsqlDbType oid)
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<PqsqlTypeParameter>() == null || Contract.Result<PqsqlTypeParameter>().TypeCode != TypeCode.Empty);
			Contract.Ensures(Contract.Result<PqsqlTypeParameter>() == null || Contract.Result<PqsqlTypeParameter>().ArrayDbType != 0);
#endif

			PqsqlTypeEntry result;
			return mPqsqlDbTypeDict.TryGetValue(oid, out result) ? result.TypeParameter : null;
		}

		#endregion


		#region access types for PqsqlDataReader

		// used in PqsqlDataReader.PopulateRowInfoAndOutputParameters
		internal static PqsqlTypeValue GetOrAdd(PqsqlDbType oid, PqsqlConnection connection)
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<PqsqlTypeValue>() != null);
			Contract.Ensures(Contract.Result<PqsqlTypeValue>().GetValue != null);
			Contract.Ensures(Contract.Result<PqsqlTypeValue>().DataTypeName != null);
			Contract.Ensures(Contract.Result<PqsqlTypeValue>().ProviderType != null);
#endif

			PqsqlTypeEntry result;
			Dictionary<PqsqlDbType, PqsqlTypeEntry> userTypes;

			// try to get native postgres type
			if (mPqsqlDbTypeDict.TryGetValue(oid, out result))
			{
#if CODECONTRACTS
				Contract.Assume(result != null);
				Contract.Assume(result.TypeValue != null);
#endif
				return result.TypeValue;
			}

			// TODO cache maintainance in mUserTypesDict not implemented

			string connectionString = connection.ConnectionString;

			// try to get user-defined datatype (CREATE TYPE, etc.), whose oid might differ between databases
			if (mUserTypesDict.TryGetValue(connectionString, out userTypes))
			{
				lock (userTypes)
				{
					if (!userTypes.TryGetValue(oid, out result))
					{
						// if oid is not yet stored, try to find it
						result = FetchType(oid, connection);
						userTypes[oid] = result; // store fresh PqsqlTypeEntry here
					}

#if CODECONTRACTS
					Contract.Assume(result != null);
					Contract.Assume(result.TypeValue != null);
#endif

					return result.TypeValue;
				}
			}

			// create fresh user-defined data type mapping
			userTypes = new Dictionary<PqsqlDbType, PqsqlTypeEntry>();

			// no user-defined data type mapping for connectionString stored yet, try to add a new one
			if (mUserTypesDict.TryAdd(connectionString, userTypes))
			{
				lock (userTypes)
				{
					if (!userTypes.TryGetValue(oid, out result))
					{
						// if oid is not yet stored, we came first, just try to find oid
						result = FetchType(oid, connection);
						userTypes[oid] = result; // store fresh PqsqlTypeEntry here
					}

#if CODECONTRACTS
					Contract.Assert(result != null);
					Contract.Assert(result.TypeValue != null);
#endif

					return result.TypeValue;
				}
			}

			// in the meantime, another thread already stored a new dynamic type mapping for connectionString
			if (mUserTypesDict.TryGetValue(connectionString, out userTypes))
			{
				lock (userTypes)
				{
					if (!userTypes.TryGetValue(oid, out result))
					{
						// if oid is not yet stored, we ran before the TryAdd thread, just try to find oid
						result = FetchType(oid, connection);
						userTypes[oid] = result; // store fresh PqsqlTypeEntry here
					}

#if CODECONTRACTS
					Contract.Assume(result != null);
					Contract.Assume(result.TypeValue != null);
#endif

					return result.TypeValue;
				}
			}

			// 
			throw new PqsqlException("Could not find datatype " + oid + " for connection " + connectionString);
		}

		// create new PqsqlTypeEntry for oid in connection
		private static PqsqlTypeEntry FetchType(PqsqlDbType oid, PqsqlConnection connection)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentOutOfRangeException>(oid != 0, "Datatype with oid=0 (InvalidOid) not supported");
			Contract.Requires<ArgumentNullException>(connection != null);

			Contract.Ensures(Contract.Result<PqsqlTypeEntry>() != null);
			Contract.Ensures(Contract.Result<PqsqlTypeEntry>().TypeValue != null);
			Contract.Ensures(Contract.Result<PqsqlTypeEntry>().TypeValue.GetValue != null);
			Contract.Ensures(Contract.Result<PqsqlTypeEntry>().TypeValue.DataTypeName != null);
			Contract.Ensures(Contract.Result<PqsqlTypeEntry>().TypeValue.ProviderType != null);
			Contract.Ensures(Contract.Result<PqsqlTypeEntry>().TypeParameter != null);
			Contract.Ensures(Contract.Result<PqsqlTypeEntry>().TypeParameter.SetValue != null);
			Contract.Ensures(Contract.Result<PqsqlTypeEntry>().TypeParameter.SetArrayItem != null);
#else
			if (oid == 0)
				throw new ArgumentOutOfRangeException(nameof(oid), "Datatype with oid=0 (InvalidOid) not supported");
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
#endif

			string connectionString = connection.ConnectionString;

			// try to guess the type mapping
			// we must open a new connection here, since we have already a running query when we call FetchType
			// TODO when we have query pipelining, we might not need to open fresh connections here https://commitfest.postgresql.org/10/634/ http://2ndquadrant.github.io/postgres/libpq-batch-mode.html 
			using (PqsqlConnection conn = new PqsqlConnection(connectionString))
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
								throw new PqsqlException("Could not fetch datatype " + oid + " for connection " + connectionString);

							// assume that we can use this type just like PqsqlDbType.Text (e.g., citext)
							PqsqlTypeEntry tn = new PqsqlTypeEntry {
								TypeValue = new PqsqlTypeValue {
									DataTypeName = typname,
									ProviderType = typeof(string),
									GetValue = (res, row, ord, typmod) => PqsqlDataReader.GetString(res, row, ord),
								},
								TypeParameter = new PqsqlTypeParameter {
									TypeCode = TypeCode.String,
									ArrayDbType = PqsqlDbType.Array,
									SetValue = PqsqlParameterBuffer.SetText,
									SetArrayItem = PqsqlParameterBuffer.SetTextArray
								},
								DbType = DbType.String,
							};

							return tn;
						}
						
						if (typcategory == 'C')
						{
							string typname = reader.GetString(1);

							if (typname == null)
								throw new PqsqlException("Could not fetch datatype " + oid + " for connection " + connectionString);
							


							if (typname == "datetimeoffset")
							{
								PqsqlTypeEntry tn = new PqsqlTypeEntry
								{
									TypeValue = new PqsqlTypeValue
									{
										DataTypeName = typname,
										ProviderType = typeof(DateTimeOffset),
										GetValue = (res, row, ord, typmod) => PqsqlDataReader.GetDateTimeOffset(res, row, ord),
									},
									TypeParameter = new PqsqlTypeParameter
									{
										TypeCode = (TypeCode) PqsqlTypeCode.DateTimeOffset,
										ArrayDbType = PqsqlDbType.Array,
										SetValue = PqsqlParameterBuffer.SetDateTimeOffset,
										SetArrayItem = null,
									},
									DbType = DbType.DateTimeOffset,
								};

								DateTimeOffsetOid = oid;
								return tn;
							}
						}

						// TODO other types not implemented
					}
				}
			}

			throw new NotSupportedException("Datatype " + oid + " not supported for connection " + connectionString);
		}

		#endregion


		#region access types for PqsqlParameter

		// used in PqsqlParameter.DbType
		internal static PqsqlDbType GetPqsqlDbType(DbType dbType)
		{
#if CODECONTRACTS
			Contract.Assume((int) dbType < mDbTypeArray.Length);
#endif
			return mDbTypeArray[(int) dbType];
		}

		// used in PqsqlParameter.PqsqlDbType
		internal static DbType GetDbType(PqsqlDbType oid)
		{
			PqsqlTypeEntry result;

			if (!mPqsqlDbTypeDict.TryGetValue(oid, out result))
			{
				// do not try to fetch datatype specs with PqsqlTypeRegistry.FetchType() here, just bail out
				throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Datatype {0} is not supported", oid & ~PqsqlDbType.Array));
			}

			return result.DbType;
		}

		#endregion
	}
}
