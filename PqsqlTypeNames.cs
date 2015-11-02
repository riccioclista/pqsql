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
		class PqsqlTypeName
		{
			public string Name {	get; set; }
			public Type Type { get;	set; }
			public DbType DbType { get;	set; }
			public Func<IntPtr,int,int,int,object> GetValue	{	get; set; }
			public Action<IntPtr,object> SetValue	{	get; set; }
		}

		// maps PqsqlDbType to PqsqlTypeName
		static readonly Dictionary<PqsqlDbType, PqsqlTypeName> mPqsqlDbTypeDict = new Dictionary<PqsqlDbType, PqsqlTypeName>
    {
			{ PqsqlDbType.Boolean,
				new PqsqlTypeName { 
					Name="bool",
					Type=typeof(bool),
					DbType=DbType.Boolean,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetBoolean(res,row,ord); },
					SetValue=(IntPtr pb,object val) => PqsqlBinaryFormat.pqbf_set_bool(pb, (int) val)
				}
			},
			{ PqsqlDbType.Float8,
				new PqsqlTypeName {
					Name="float8",
					Type=typeof(double),
					DbType=DbType.Double,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetDouble(res,row,ord); },
					SetValue=(IntPtr pb,object val) => PqsqlBinaryFormat.pqbf_set_float8(pb, (double) val)
				}
			},
			{ PqsqlDbType.Int4,
				new PqsqlTypeName {
					Name="int4",
					Type=typeof(int),
					DbType=DbType.Int32,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetInt32(res,row,ord); },
					SetValue=(IntPtr pb,object val) => PqsqlBinaryFormat.pqbf_set_int4(pb, (int) val)
				}
			},
			{ PqsqlDbType.Numeric,
				new PqsqlTypeName {
					Name="numeric",
					Type=typeof(Decimal),
					DbType=DbType.VarNumeric,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetNumeric(res,row,ord,typmod); },
					SetValue=(IntPtr pb,object val) => {
						double d = Convert.ToDouble(val);
						PqsqlBinaryFormat.pqbf_set_numeric(pb, d);
					}
				}
			},
			{ PqsqlDbType.Float4,
				new PqsqlTypeName {
					Name="float4",
					Type=typeof(float),
					DbType=DbType.Single,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetFloat(res,row,ord); },
					SetValue=(IntPtr pb,object val) => PqsqlBinaryFormat.pqbf_set_float4(pb, (float) val)
				}
			},
			{ PqsqlDbType.Int2,
				new PqsqlTypeName {
					Name="int2",
					Type=typeof(short),
					DbType=DbType.Int16,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetInt16(res,row,ord); },
					SetValue=(IntPtr pb,object val) => PqsqlBinaryFormat.pqbf_set_int2(pb, (short) val)
				}
			},
			{ PqsqlDbType.Char,
				new PqsqlTypeName {
					Name="char",
					Type=typeof(char[]),
					DbType=DbType.StringFixedLength,
					GetValue=null,
					SetValue=null
				}
			},
			{ PqsqlDbType.Text,
				new PqsqlTypeName {
					Name="text",
					Type=typeof(string),
					DbType=DbType.String,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetString(res,row,ord); },
					SetValue=(IntPtr pb,object val) => { unsafe { fixed (char* t = (string) val) { PqsqlBinaryFormat.pqbf_set_unicode_text(pb, t); } } }
				}
			},
			{ PqsqlDbType.Varchar,
				new PqsqlTypeName {
					Name="varchar",
					Type=typeof(string),
					DbType=DbType.String,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetString(res,row,ord); },
					SetValue=(IntPtr pb,object val) => { unsafe { fixed (char* t = (string) val) { PqsqlBinaryFormat.pqbf_set_unicode_text(pb, t); } } }
				}
			},
			{ PqsqlDbType.Name,
				new PqsqlTypeName {
					Name="name",
					Type=typeof(string),
					DbType=DbType.StringFixedLength,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetString(res,row,ord); },
					SetValue=(IntPtr pb,object val) => { unsafe { fixed (char* t = (string) val) { PqsqlBinaryFormat.pqbf_set_unicode_text(pb, t); } } }
				}
			},
			{ PqsqlDbType.Bytea,
				new PqsqlTypeName {
					Name="bytea",
					Type=typeof(byte[]),
					DbType=DbType.Object,
					GetValue=null,
					SetValue=null
				}
			},
			{ PqsqlDbType.Date,
				new PqsqlTypeName {
					Name="date",
					Type=typeof(DateTime),
					DbType=DbType.Date,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetDate(res,row,ord); },
					SetValue=null // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_set_date(pb, (DateTime) val); }
				}
			},
			{ PqsqlDbType.Time,
				new PqsqlTypeName {
					Name="time",
					Type=typeof(DateTime),
					DbType=DbType.Time,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetTime(res,row,ord); },
					SetValue=null // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_set_date(pb, (DateTime) val); }
				}
			},
			{ PqsqlDbType.Timestamp,
				new PqsqlTypeName {
					Name="timestamp",
					Type=typeof(DateTime),
					DbType=DbType.DateTime,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetDateTime(res,row,ord); },
					SetValue=(IntPtr pb,object val) => {
						DateTime dt = (DateTime) val;
						long ticks = dt.Ticks;
						long sec = ticks / TimeSpan.TicksPerSecond;
						long usec = ticks / TimeSpan.TicksPerMillisecond;
						PqsqlBinaryFormat.pqbf_set_timestamp(pb, sec, usec / 10);
					}
				}
			},
			{ PqsqlDbType.TimestampTZ,
				new PqsqlTypeName {
					Name="timestamptz",
					Type=typeof(DateTime),
					DbType=DbType.DateTimeOffset,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetDateTime(res,row,ord); },
					SetValue=null // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_set_timestamp(pb, (DateTime) val); }
				}
			},
			{ PqsqlDbType.Interval,
				new PqsqlTypeName {
					Name="interval",
					Type=typeof(TimeSpan),
					DbType=DbType.DateTime,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetInterval(res,row,ord); },
					SetValue=null // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_set_interval(pb, (DateTime) val); }
				}
			},
			{ PqsqlDbType.TimeTZ,
				new PqsqlTypeName {
					Name="timetz",
					Type=typeof(DateTime),
					DbType=DbType.DateTime,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetTime(res,row,ord); },
					SetValue=null // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_set_date(pb, (DateTime) val); }
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
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetGuid(res,row,ord); },
					SetValue=null // TODO (IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_set_uuid(pb, (Guid) val); }
				}
			},
			{ PqsqlDbType.Refcursor,
				new PqsqlTypeName {
					Name="refcursor",
					Type=typeof(object),
					DbType=DbType.Object,
					GetValue=null,
					SetValue=null
				}
			},
			{ PqsqlDbType.Oid,
				new PqsqlTypeName {
					Name="oid",
					Type=typeof(uint),
					DbType=DbType.UInt32,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return (uint) PqsqlDataReader.GetInt32(res,row,ord); },
					SetValue=(IntPtr pb,object val) => { PqsqlBinaryFormat.pqbf_set_oid(pb, (uint) val); }
				}
			},
			{ PqsqlDbType.Unknown,
				new PqsqlTypeName {
					Name="unknown",
					Type=typeof(string),
					DbType=DbType.String,
					GetValue=(IntPtr res,int row,int ord,int typmod) => { return PqsqlDataReader.GetString(res,row,ord); },
					SetValue=(IntPtr pb,object val) => { unsafe { fixed (char* t = (string) val) { PqsqlBinaryFormat.pqbf_set_unicode_text(pb, t); } } }
				}
			}
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


		static PqsqlTypeName Get(PqsqlDbType oid)
		{
			PqsqlTypeName result;
			if (mPqsqlDbTypeDict.TryGetValue(oid, out result))
			{
				return result;
			}
			throw new NotSupportedException("Datatype not supported");
		}

		public static string GetName(PqsqlDbType oid)
		{
			return Get(oid).Name;
		}

		public static Type GetType(PqsqlDbType oid)
		{
			return Get(oid).Type;
		}

		public static Func<IntPtr, int, int, int, object> GetValue(PqsqlDbType oid)
		{
			return Get(oid).GetValue;
		}

		public static DbType GetDbType(PqsqlDbType oid)
		{
			return Get(oid).DbType;
		}

		public static Action<IntPtr, object> SetValue(PqsqlDbType oid)
		{
			return Get(oid).SetValue;
		}

		public static PqsqlDbType GetPqsqlDbType(DbType dbType)
		{
			return mDbTypeArray[(int)dbType];
		}
	}
}
