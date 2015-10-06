using System;
using System.Collections.Generic;
using System.Data;

namespace Pqsql
{
	/// <summary>
	/// Static dictionary of Postgres types, datatype names, and GetValue() delegates
	/// </summary>
	internal static class PqsqlTypeNames
	{
		class PqsqlTypeName
		{
			public string Name {	get; set; }
			public Type Type { get;	set; }
			public Func<IntPtr,int,int,object> GetValue	{	get; set; }
			public DbType DbType {	get; set; }
		}

		static Dictionary<PqsqlDbType, PqsqlTypeName> mDict = new Dictionary<PqsqlDbType, PqsqlTypeName>
    {
			{ PqsqlDbType.Boolean,
				new PqsqlTypeName { 
					Name="bool",
					Type=typeof(bool),
					DbType=DbType.Boolean,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetBoolean(res,row,ord); }
				}
			},
			{ PqsqlDbType.Float8,
				new PqsqlTypeName {
					Name="float8",
					Type=typeof(double),
					DbType=DbType.Double,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDouble(res,row,ord); }
				}
			},
			{ PqsqlDbType.Int4,
				new PqsqlTypeName {
					Name="int4",
					Type=typeof(int),
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetInt32(res,row,ord); },
					DbType=DbType.Int32
				}
			},
			{ PqsqlDbType.Numeric,
				new PqsqlTypeName {
					Name="numeric",
					Type=typeof(Decimal),
					DbType=DbType.VarNumeric,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDecimal(res,row,ord); }
				}
			},
			{ PqsqlDbType.Float4,
				new PqsqlTypeName {
					Name="float4",
					Type=typeof(float),
					DbType=DbType.Single,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetFloat(res,row,ord); }
				}
			},
			{ PqsqlDbType.Int2,
				new PqsqlTypeName {
					Name="int2",
					Type=typeof(short),
					DbType=DbType.Int16,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetInt16(res,row,ord); }
				}
			},
			{ PqsqlDbType.Char,
				new PqsqlTypeName {
					Name="char",
					Type=typeof(char[]),
					DbType=DbType.StringFixedLength,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetByte(res,row,ord); }
				}
			},
			{ PqsqlDbType.Text,
				new PqsqlTypeName {
					Name="text",
					Type=typeof(string),
					DbType=DbType.String,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetString(res,row,ord); }
				}
			},
			{ PqsqlDbType.Varchar,
				new PqsqlTypeName {
					Name="varchar",
					Type=typeof(string),
					DbType=DbType.String,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetString(res,row,ord); }
				}
			},
			{ PqsqlDbType.Name,
				new PqsqlTypeName {
					Name="name",
					Type=typeof(string),
					DbType=DbType.StringFixedLength,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetString(res,row,ord); }
				}
			},
			{ PqsqlDbType.Bytea,
				new PqsqlTypeName {
					Name="bytea",
					Type=typeof(byte[]),
					DbType=DbType.Object,
					GetValue=null
				}
			},
			{ PqsqlDbType.Date,
				new PqsqlTypeName {
					Name="date",
					Type=typeof(DateTime),
					DbType=DbType.Date,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDate(res,row,ord); }
				}
			},
			{ PqsqlDbType.Time,
				new PqsqlTypeName {
					Name="time",
					Type=typeof(DateTime),
					DbType=DbType.Time,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetTime(res,row,ord); }
				}
			},
			{ PqsqlDbType.Timestamp,
				new PqsqlTypeName {
					Name="timestamp",
					Type=typeof(DateTime),
					DbType=DbType.DateTime,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDateTime(res,row,ord); }
				}
			},
			{ PqsqlDbType.TimestampTZ,
				new PqsqlTypeName {
					Name="timestamptz",
					Type=typeof(DateTime),
					DbType=DbType.DateTimeOffset,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDateTime(res,row,ord); }
				}
			},
			{ PqsqlDbType.Interval,
				new PqsqlTypeName {
					Name="interval",
					Type=typeof(TimeSpan),
					DbType=DbType.DateTime,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDateTime(res,row,ord); }
				}
			},
			{ PqsqlDbType.TimeTZ,
				new PqsqlTypeName {
					Name="timetz",
					Type=typeof(DateTime),
					DbType=DbType.Time,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetDateTime(res,row,ord); }
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
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetGuid(res,row,ord); }
				}
			},
			{ PqsqlDbType.Refcursor,
				new PqsqlTypeName {
					Name="refcursor",
					Type=typeof(object),
					DbType=DbType.Object,
					GetValue=null
				}
			},
			{ PqsqlDbType.Oid,
				new PqsqlTypeName {
					Name="oid",
					Type=typeof(uint),
					DbType=DbType.UInt32,
					GetValue=delegate(IntPtr res,int row,int ord) { return (uint) PqsqlDataReader.GetInt32(res,row,ord); }
				}
			},
			{ PqsqlDbType.Unknown,
				new PqsqlTypeName {
					Name="unknown",
					Type=typeof(string),
					DbType=DbType.String,
					GetValue=delegate(IntPtr res,int row,int ord) { return PqsqlDataReader.GetString(res,row,ord); }
				}
			}
    };


		static PqsqlTypeName Get(PqsqlDbType oid)
		{
			PqsqlTypeName result;
			if (mDict.TryGetValue(oid, out result))
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

		public static Func<IntPtr, int, int, object> GetValue(PqsqlDbType oid)
		{
			return Get(oid).GetValue;
		}

		public static DbType GetDbType(PqsqlDbType oid)
		{
			return Get(oid).DbType;
		}
	}
}
