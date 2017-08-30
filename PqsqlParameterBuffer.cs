using System;
using System.Data;
using System.Diagnostics.Contracts;
using System.Globalization;

using PqsqlBinaryFormat = Pqsql.UnsafeNativeMethods.PqsqlBinaryFormat;

namespace Pqsql
{
	internal sealed class PqsqlParameterBuffer : IDisposable
	{
		// pqparam_buffer* for Input and InputOutput parameter
		private IntPtr mPqPB;

		// Summary:
		//     Initializes a new instance of the System.Data.Common.DbParameterCollection
		//     class.
		public PqsqlParameterBuffer()
		{
			Init();
		}

		public PqsqlParameterBuffer(PqsqlParameterCollection parameterCollection)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(parameterCollection != null);
#else
			if (parameterCollection == null)
				throw new ArgumentNullException(nameof(parameterCollection));
#endif

			Init();

			AddParameterCollection(parameterCollection);
		}

		private void Init()
		{
			mPqPB = PqsqlBinaryFormat.pqpb_create(); // create pqparam_buffer
			if (mPqPB == null)
				throw new PqsqlException("Cannot create buffer for parameter collection");
		}

		#region IDisposable

		~PqsqlParameterBuffer()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool mDisposed;

		private void Dispose(bool disposing)
		{
			if (mDisposed)
			{
				return;
			}

			if (disposing)
			{
				// always dispose parameter buffer
			}

			if (mPqPB != IntPtr.Zero)
			{
				PqsqlBinaryFormat.pqpb_free(mPqPB);
				mPqPB = IntPtr.Zero;
			}
			mDisposed = true;
		}

		#endregion


		#region pqparam_buffer* setup

		public int GetQueryParams(out IntPtr ptyps, out IntPtr pvals, out IntPtr plens, out IntPtr pfrms)
		{
			int num_param = 0;
			ptyps = IntPtr.Zero; // oid*
			pvals = IntPtr.Zero; // char**
			plens = IntPtr.Zero; // int*
			pfrms = IntPtr.Zero; // int*

			if (mPqPB != IntPtr.Zero)
			{
				num_param = PqsqlBinaryFormat.pqpb_get_num(mPqPB);
				ptyps = PqsqlBinaryFormat.pqpb_get_types(mPqPB);
				pvals = PqsqlBinaryFormat.pqpb_get_vals(mPqPB);
				plens = PqsqlBinaryFormat.pqpb_get_lens(mPqPB);
				pfrms = PqsqlBinaryFormat.pqpb_get_frms(mPqPB);
			}

			return num_param;
		}

		/// <summary>
		/// append each PqsqlParameter from the PqsqlParameterCollection to the parameter buffer.
		/// </summary>
		public void AddParameterCollection(PqsqlParameterCollection parameterCollection)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(parameterCollection != null);
#else
			if (parameterCollection == null)
				throw new ArgumentNullException(nameof(parameterCollection));
#endif

			foreach (PqsqlParameter p in parameterCollection)
			{
				AddParameter(p);
			}
		}

		/// <summary>
		/// append parameter to parameter buffer.
		/// we convert and infer the right datatype in case the user supplied inconsistent type information.
		/// </summary>
		public void AddParameter(PqsqlParameter parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException(nameof(parameter));

			ParameterDirection direction = parameter.Direction;
			// skip output parameters and return values
			if (direction == ParameterDirection.Output || direction == ParameterDirection.ReturnValue)
				return;

			PqsqlDbType oid = parameter.PqsqlDbType;
			object v = parameter.Value;
			bool vNotNull = v != null && v != DBNull.Value;
			TypeCode vtc = Convert.GetTypeCode(v);

			// no PqsqlDbType set by the user, try to infer datatype from Value and set new oid
			// if v is null or DBNull.Value, we can work with PqsqlDbType.Unknown
			if (oid == PqsqlDbType.Unknown && vNotNull)
			{
				if (vtc != TypeCode.Object)
				{
					oid = InferValueType(vtc);
				}
				else if (v is DateTimeOffset)
				{
					oid = PqsqlDbType.TimestampTZ;
				}
				else if (v is byte[])
				{
					oid = PqsqlDbType.Bytea;
				}
				else if (v is Guid)
				{
					oid = PqsqlDbType.Uuid;
				}
				else if (v is TimeSpan)
				{
					oid = PqsqlDbType.Interval;
				}

				if (oid == PqsqlDbType.Unknown) // cannot resolve oid for non-null v
				{
					throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Could not infer datatype for PqsqlParameter {0} (TypeCode={1})", parameter.ParameterName, vtc));
				}
			}

			// get SetValue / SetArrayItem delegates for oid
			PqsqlTypeRegistry.PqsqlTypeParameter tp = PqsqlTypeRegistry.Get(oid & ~PqsqlDbType.Array);
			if (tp == null)
			{
				// do not try to fetch datatype specs with PqsqlTypeRegistry.FetchType() here, just bail out
				throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Datatype {0} is not supported", oid & ~PqsqlDbType.Array));
			}

			// try to convert to the proper datatype in case the user supplied a wrong PqsqlDbType
			// if v is null or DBNull.Value, we can work with PqsqlDbType.Unknown
			if (vNotNull && (oid & PqsqlDbType.Array) != PqsqlDbType.Array)
			{
				TypeCode tc = tp.TypeCode;

				if (vtc != TypeCode.Empty && vtc != tc)
				{
					v = ConvertParameterValue(v, vtc, tc, oid);
				}
			}

			// add parameter to the parameter buffer
			AddParameterValue(tp, oid, v);
		}

		// dispatch parameter type and add it to parameter buffer mPqPB
		private void AddParameterValue(PqsqlTypeRegistry.PqsqlTypeParameter tp, PqsqlDbType oid, object v)
		{
#if CODECONTRACTS
			Contract.Assume(mPqPB != IntPtr.Zero);
#endif

			if (v == null || v == DBNull.Value)
			{
				// null arrays must have oid of element type
				PqsqlBinaryFormat.pqbf_add_null(mPqPB, (uint) (oid & ~PqsqlDbType.Array));
			}
			else if ((oid & PqsqlDbType.Array) == PqsqlDbType.Array)
			{
				SetArrayValue(mPqPB, v, oid, tp);
			}
			else
			{
#if CODECONTRACTS
				Contract.Assume(tp.SetValue != null);
#endif

				tp.SetValue(mPqPB, v, oid);
			}
		}

		// convert v of typecode vtc to typecode dtc
		private static object ConvertParameterValue(object v, TypeCode vtc, TypeCode dtc, PqsqlDbType oid)
		{
			if (vtc == TypeCode.String && string.IsNullOrEmpty(v as string))
			{
				// we got an empty string that does not match the target type: we simply
				// ignore the value and return null, as the conversion wouldn't work
				return null;
			}

			if (dtc == TypeCode.DateTime && oid == PqsqlDbType.TimestampTZ)
			{
				// use UTC in case we want to convert DateTimeOffset to DateTime
				DateTimeOffset off = (DateTimeOffset) v;
				v = off.UtcDateTime;
			}

			// in case we would have an invalid cast from object to target type
			// we try to convert v to the registered ProviderType next
			return Convert.ChangeType(v, dtc, CultureInfo.InvariantCulture);
		}

		// try to infer PqsqlDbType from TypeCode
		private static PqsqlDbType InferValueType(TypeCode vtc)
		{
			PqsqlDbType oid;

			switch (vtc)
			{
			case TypeCode.String:
				oid = PqsqlDbType.Text;
				break;
			case TypeCode.Boolean:
				oid = PqsqlDbType.Boolean;
				break;
			case TypeCode.DateTime:
				oid = PqsqlDbType.Timestamp;
				break;
			case TypeCode.Decimal:
				oid = PqsqlDbType.Numeric;
				break;
			case TypeCode.SByte:
				oid = PqsqlDbType.Char;
				break;
			case TypeCode.Single:
				oid = PqsqlDbType.Float4;
				break;
			case TypeCode.Double:
				oid = PqsqlDbType.Float8;
				break;
			case TypeCode.Int16:
				oid = PqsqlDbType.Int2;
				break;
			case TypeCode.Int32:
				oid = PqsqlDbType.Int4;
				break;
			case TypeCode.Int64:
				oid = PqsqlDbType.Int8;
				break;

			//case TypeCode.Empty:
			//case TypeCode.Object:
			//case TypeCode.DBNull:
			//case TypeCode.Char:
			//case TypeCode.Byte:
			//case TypeCode.UInt16:
			//case TypeCode.UInt32:
			//case TypeCode.UInt64:
			default:
				oid = PqsqlDbType.Unknown;
				break;
			}

			return oid;
		}

		#endregion

		

		//
		// Summary:
		//     Removes all System.Data.Common.DbParameter values from the System.Data.Common.DbParameterCollection.
		public void Clear()
		{
			if (mPqPB != IntPtr.Zero)
			{
				PqsqlBinaryFormat.pqpb_reset(mPqPB);
			}
		}


		#region add item to array

		// adds array item length prefix and then invokes set
		internal static void SetTypeArray<T>(IntPtr a, int length, Action<IntPtr,T> set, T v) where T : struct
		{
			PqsqlBinaryFormat.pqbf_set_array_itemlength(a, length);
			set(a, v);
		}

		// adds o as double array element to PQExpBuffer a
		internal static void SetNumericArray(IntPtr a, object o)
		{
			double d = Convert.ToDouble(o, CultureInfo.InvariantCulture);

			long len0 = PqsqlBinaryFormat.pqbf_get_buflen(a); // get start position

			PqsqlBinaryFormat.pqbf_set_array_itemlength(a, -2); // first set an invalid item length
			PqsqlBinaryFormat.pqbf_set_numeric(a, d); // encode numeric value (variable length)

			int len = (int) (PqsqlBinaryFormat.pqbf_get_buflen(a) - len0); // get new buffer length
			// update array item length == len - 4 bytes
			PqsqlBinaryFormat.pqbf_update_array_itemlength(a, -len, len - 4);
		}

		// adds o as string array element to PQExpBuffer a
		internal static void SetTextArray(IntPtr a, object o)
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
		}

		// adds o as DateTime array element into PQExpBuffer a
		internal static void SetTimestampArray(IntPtr a, object o)
		{
			DateTime dt = (DateTime) o;

			long sec;
			int usec;
			PqsqlBinaryFormat.GetTimestamp(dt, out sec, out usec);

			PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
			PqsqlBinaryFormat.pqbf_set_timestamp(a, sec, usec);
		}

		// adds o as TimeSpan array element into PQExpBuffer a
		internal static void SetIntervalArray(IntPtr a, object o)
		{
			TimeSpan ts = (TimeSpan)o;

			long offset;
			int day;
			int month;
			PqsqlBinaryFormat.GetInterval(ts, out offset, out day, out month);

			PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 16);
			PqsqlBinaryFormat.pqbf_set_interval(a, offset, day, month);
		}

		private static void SetArrayValue(IntPtr pb, object val, PqsqlDbType oid, PqsqlTypeRegistry.PqsqlTypeParameter n)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(n != null);
#else
			if (n == null)
				throw new ArgumentNullException(nameof(n));
#endif

			oid &= ~PqsqlDbType.Array; // remove Array flag
			PqsqlDbType arrayoid = n.ArrayDbType;
			Action<IntPtr, object> setArrayItem = n.SetArrayItem;

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
				a = UnsafeNativeMethods.PqsqlWrapper.createPQExpBuffer();

				if (a == IntPtr.Zero)
					throw new PqsqlException("Cannot create buffer for array parameter");

				// check for null values
				int hasNulls = 0;
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
					UnsafeNativeMethods.PqsqlWrapper.destroyPQExpBuffer(a);
			}
		}

		#endregion


		#region add item to parameter buffer

		// sets val as string with Oid oid (PqsqlDbType.BPChar, PqsqlDbType.Text, PqsqlDbType.Varchar, PqsqlDbType.Name, PqsqlDbType.Char)
		// into pqparam_buffer pb
		internal static unsafe void SetText(IntPtr pb, object val, PqsqlDbType oid)
		{
			fixed (char* t = (string) val)
			{
				PqsqlBinaryFormat.pqbf_add_unicode_text(pb, t, (uint) oid);
			}
		}

		// sets val as DateTime with Oid oid (PqsqlDbType.Timestamp, PqsqlDbType.TimestampTZ) into pqparam_buffer pb
		internal static void SetTimestamp(IntPtr pb, object val, PqsqlDbType oid)
		{
			DateTime dt = (DateTime) val;

			long sec;
			int usec;
			PqsqlBinaryFormat.GetTimestamp(dt, out sec, out usec);

			PqsqlBinaryFormat.pqbf_add_timestamp(pb, sec, usec, (uint) oid);
		}

		// sets val as TimeSpan into pqparam_buffer pb
		internal static void SetInterval(IntPtr pb, object val, PqsqlDbType oid)
		{
			TimeSpan ts = (TimeSpan)val;

			long offset;
			int day;
			int month;
			PqsqlBinaryFormat.GetInterval(ts, out offset, out day, out month);

			PqsqlBinaryFormat.pqbf_add_interval(pb, offset, day, month);
		}

		#endregion
	}
}