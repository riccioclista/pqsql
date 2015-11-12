using System;
using System.Collections.Generic;
using System.Data;

namespace Pqsql
{
	/// <summary>
	/// Static dictionary of Postgres types, datatype names, GetValue() / SetValue() delegates
	/// </summary>
	internal static class PqsqlTypeNames
	{
		public sealed class PqsqlTypeName
		{
			public string Name {	get; set; }
			public Type Type { get;	set; }
			public DbType DbType { get;	set; }
			public PqsqlDbType ArrayDbType { get; set; }
			public Func<IntPtr,int,int,int,object> GetValue	{	get; set; }
			public Action<IntPtr,object> SetValue	{	get; set; }
			public Action<IntPtr,object> SetArrayValue { get; set; }
		}

		// maps PqsqlDbType to PqsqlTypeName
		static readonly Dictionary<PqsqlDbType, PqsqlTypeName> mPqsqlDbTypeDict = new Dictionary<PqsqlDbType, PqsqlTypeName>
    {
			{ PqsqlDbType.Boolean,
				new PqsqlTypeName { 
					Name="bool",
					Type=typeof(bool),
					DbType=DbType.Boolean,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetBoolean(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_bool(pb, (int) val),
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Float8,
				new PqsqlTypeName {
					Name="float8",
					Type=typeof(double),
					DbType=DbType.Double,
					ArrayDbType=PqsqlDbType.Float8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDouble(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_float8(pb, (double) val),
					SetArrayValue = (a, o) => {
						double v = (double) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
						PqsqlBinaryFormat.pqbf_set_float8(a, v);
					}
				}
			},
			{ PqsqlDbType.Int4,
				new PqsqlTypeName {
					Name="int4",
					Type=typeof(int),
					DbType=DbType.Int32,
					ArrayDbType=PqsqlDbType.Int4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt32(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_int4(pb, (int) val),
					SetArrayValue = (a, o) => {
						int v = (int) o;
						PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 4);
						PqsqlBinaryFormat.pqbf_set_int4(a, v);
					}
				}
			},
			{ PqsqlDbType.Int8,
				new PqsqlTypeName {
					Name="int8",
					Type=typeof(long),
					DbType=DbType.Int64,
					ArrayDbType=PqsqlDbType.Int8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt64(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_int8(pb, (long) val),
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Numeric,
				new PqsqlTypeName {
					Name="numeric",
					Type=typeof(Decimal),
					DbType=DbType.VarNumeric,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetNumeric(res,row,ord,typmod),
					SetValue=(pb, val) => {
						double d = Convert.ToDouble(val);
						PqsqlBinaryFormat.pqbf_add_numeric(pb, d);
					},
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Float4,
				new PqsqlTypeName {
					Name="float4",
					Type=typeof(float),
					DbType=DbType.Single,
					ArrayDbType=PqsqlDbType.Float4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetFloat(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_float4(pb, (float) val),
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Int2,
				new PqsqlTypeName {
					Name="int2",
					Type=typeof(short),
					DbType=DbType.Int16,
					ArrayDbType=PqsqlDbType.Int2Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt16(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_int2(pb, (short) val),
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Char,
				new PqsqlTypeName {
					Name="char",
					Type=typeof(char[]),
					DbType=DbType.StringFixedLength,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=null,
					SetValue=null,
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Text,
				new PqsqlTypeName {
					Name="text",
					Type=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.TextArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue=(pb, val) => { unsafe { fixed (char* t = (string) val) { PqsqlBinaryFormat.pqbf_add_unicode_text(pb, t); } } },
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Varchar,
				new PqsqlTypeName {
					Name="varchar",
					Type=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.TextArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue=(pb, val) => { unsafe { fixed (char* t = (string) val) { PqsqlBinaryFormat.pqbf_add_unicode_text(pb, t); } } },
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Name,
				new PqsqlTypeName {
					Name="name",
					Type=typeof(string),
					DbType=DbType.StringFixedLength,
					ArrayDbType=PqsqlDbType.TextArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue=(pb, val) => { unsafe { fixed (char* t = (string) val) { PqsqlBinaryFormat.pqbf_add_unicode_text(pb, t); } } },
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Bytea,
				new PqsqlTypeName {
					Name="bytea",
					Type=typeof(byte[]),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=null, // TODO (res, row, ord, typmod) => PqsqlDataReader.GetDate(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_date(pb, (DateTime) val); }
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Date,
				new PqsqlTypeName {
					Name="date",
					Type=typeof(DateTime),
					DbType=DbType.Date,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDate(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_date(pb, (DateTime) val); }
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Time,
				new PqsqlTypeName {
					Name="time",
					Type=typeof(DateTime),
					DbType=DbType.Time,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetTime(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_time(pb, (DateTime) val); }
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Timestamp,
				new PqsqlTypeName {
					Name="timestamp",
					Type=typeof(DateTime),
					DbType=DbType.DateTime,
					ArrayDbType=PqsqlDbType.TimestampArray, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDateTime(res,row,ord),
					SetValue=(pb, val) => {
						DateTime dt = (DateTime) val;
						long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
						long sec = ticks / TimeSpan.TicksPerSecond;
						int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);
						PqsqlBinaryFormat.pqbf_add_timestamp(pb, sec, usec);
					},
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.TimestampTZ,
				new PqsqlTypeName {
					Name="timestamptz",
					Type=typeof(DateTime),
					DbType=DbType.DateTimeOffset,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDateTime(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_timestamptz(pb, (DateTime) val); }
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Interval,
				new PqsqlTypeName {
					Name="interval",
					Type=typeof(TimeSpan),
					DbType=DbType.DateTime,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInterval(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_interval(pb, (DateTime) val); }
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.TimeTZ,
				new PqsqlTypeName {
					Name="timetz",
					Type=typeof(DateTime),
					DbType=DbType.DateTime,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetTime(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_timetz(pb, (DateTime) val); }
					SetArrayValue = null
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
					Type=typeof(Guid),
					DbType=DbType.Guid,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetGuid(res,row,ord),
					SetValue=null, // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_add_uuid(pb, (Guid) val); }
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Refcursor,
				new PqsqlTypeName {
					Name="refcursor",
					Type=typeof(object),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Array, // TODO
					GetValue=null,
					SetValue=null,
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Oid,
				new PqsqlTypeName {
					Name="oid",
					Type=typeof(uint),
					DbType=DbType.UInt32,
					ArrayDbType=PqsqlDbType.OidArray,
					GetValue=(res, row, ord, typmod) => (uint) PqsqlDataReader.GetInt32(res,row,ord),
					SetValue=(pb, val) => PqsqlBinaryFormat.pqbf_add_oid(pb, (uint) val),
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Unknown,
				new PqsqlTypeName {
					Name="unknown",
					Type=typeof(string),
					DbType=DbType.String,
					ArrayDbType=PqsqlDbType.TextArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetString(res,row,ord),
					SetValue=(pb, val) => { unsafe { fixed (char* t = (string) val) { PqsqlBinaryFormat.pqbf_add_unicode_text(pb, t); } } },
					SetArrayValue = null
				}
			},



			{ PqsqlDbType.Int2Array,
				new PqsqlTypeName {
					Name="_int2",
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Int2Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt16Array(res, row, ord),
					SetValue=null,
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Int4Array,
				new PqsqlTypeName {
					Name="_int4",
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Int4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt32Array(res, row, ord),
					SetValue=null,
					SetArrayValue = null
				}
			},

			{ PqsqlDbType.TextArray,
				new PqsqlTypeName {
					Name="_text",
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.TextArray,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetStringArray(res,row,ord),
					SetValue=null,
					SetArrayValue = null
				}
			},

			{ PqsqlDbType.Int8Array,
				new PqsqlTypeName {
					Name="_int8",
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Int8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetInt64Array(res, row, ord),
					SetValue=null,
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Float4Array,
				new PqsqlTypeName {
					Name="_float4",
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Float4Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetFloatArray(res, row, ord),
					SetValue=null,
					SetArrayValue = null
				}
			},
			{ PqsqlDbType.Float8Array,
				new PqsqlTypeName {
					Name="_float8",
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.Float8Array,
					GetValue=(res, row, ord, typmod) => PqsqlDataReader.GetDoubleArray(res, row, ord),
					SetValue=null,
					SetArrayValue = null
				}
			},

			{ PqsqlDbType.OidArray,
				new PqsqlTypeName {
					Name="_oid",
					Type=typeof(Array),
					DbType=DbType.Object,
					ArrayDbType=PqsqlDbType.OidArray,
					GetValue=null,
					SetValue=null,
					SetArrayValue = null
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

		public static PqsqlDbType GetPqsqlArrayDbType(PqsqlDbType oid)
		{
			return Get(oid).ArrayDbType;
		}

		// for PqsqlParameterCollection
		public static Action<IntPtr, object> SetArrayValue(PqsqlDbType oid)
		{
			oid &= ~PqsqlDbType.Array;
			PqsqlTypeName n = Get(oid);
			PqsqlDbType arrayoid = n.ArrayDbType;
			Action<IntPtr, object> setArrayValue = n.SetArrayValue;

			// return closure
			return (pb, val) =>
			{
				Array tmp = val as Array;
				int rank = tmp.Rank;

				// TODO we only support one-dimensional array for now
				if (rank != 1)
					throw new NotImplementedException("only one-dimensional arrays supported");

				int[] dim = new int[rank];
				int[] lbound = new int[rank];

				for (int i = 0; i < rank; i++)
				{
					dim[i] = tmp.GetUpperBound(i);
					lbound[i] = tmp.GetLowerBound(i);
				}

				IntPtr a = IntPtr.Zero;
				try
				{
					a = PqsqlWrapper.createPQExpBuffer();

					if (a == IntPtr.Zero)
						throw new OutOfMemoryException("Cannot create PQExpBuffer");

					// create array header
					PqsqlBinaryFormat.pqbf_set_array(a, rank, /* no nulls allowed */ 0, (uint) oid, dim, lbound);

					// add array items
					for (int i = lbound[0]; i <= dim[0]; i++)
					{
						object o = tmp.GetValue(i);
						if (o == null) // null values have itemlength -1
						{
							PqsqlBinaryFormat.pqbf_set_array_itemlength(a, -1);
						}
						else
						{
							setArrayValue(a, o);
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

		public static Action<IntPtr, object> SetValue(PqsqlDbType oid)
		{
			return Get(oid).SetValue;
		}
	}
}
