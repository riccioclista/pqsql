using System;
using System.Collections.Generic;
using System.Data.Common;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Linq;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

using PqsqlBinaryFormat = Pqsql.UnsafeNativeMethods.PqsqlBinaryFormat;

namespace Pqsql
{
	public sealed class PqsqlParameterCollection : DbParameterCollection, IDisposable
	{
		// input and output parameters
		private readonly List<PqsqlParameter> mParamList = new List<PqsqlParameter>();

		// Dictionary lookups for GetValue to improve performance
		private readonly Dictionary<string, int> mLookup = new Dictionary<string, int>();

		// pqparam_buffer* for Input and InputOutput parameter
		private IntPtr mPqPB;

		// when mPqPB needs to be recreated from mParamList
		bool mChanged;

		// Summary:
		//     Initializes a new instance of the System.Data.Common.DbParameterCollection
		//     class.
		public PqsqlParameterCollection()
		{
			mPqPB = PqsqlBinaryFormat.pqpb_create(); // create pqparam_buffer
			if (mPqPB == null)
				throw new PqsqlException("Cannot create buffer for parameter collection");
		}

		#region IDisposable

		~PqsqlParameterCollection()
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

		/// <summary>
		/// create parameter buffer for statement input parameters.
		/// we convert and infer the right datatype in case the user supplied inconsistent type information.
		/// </summary>
		/// <returns>pqparam_buffer as IntPtr</returns>
		internal IntPtr CreateParameterBuffer()
		{
			if (mChanged)
			{
				PqsqlBinaryFormat.pqpb_reset(mPqPB);

				foreach (PqsqlParameter p in mParamList)
				{
					if (p.Direction == ParameterDirection.Output) // skip output parameters
						continue;

					PqsqlDbType oid = p.PqsqlDbType;
					object v = p.Value;
					TypeCode vtc = Convert.GetTypeCode(v);

					if (oid == PqsqlDbType.Unknown) // no PqsqlDbType set by the user, try to infer datatype from Value and set new oid
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

						if (oid == PqsqlDbType.Unknown) // cannot resolve oid for v
						{
							throw new PqsqlException(string.Format(CultureInfo.InvariantCulture, "Could not infer datatype for PqsqlParameter {0} (TypeCode={1})", p.ParameterName, vtc));
						}
					}

					PqsqlTypeRegistry.PqsqlTypeParameter tp = PqsqlTypeRegistry.Get(oid & ~PqsqlDbType.Array);
					if (tp == null)
					{
						// do not try to fetch datatype specs with PqsqlTypeRegistry.FetchType() here, just bail out
						throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Datatype {0} is not supported", oid & ~PqsqlDbType.Array));
					}

					// try to convert to the proper datatype in case the user supplied a wrong PqsqlDbType
					if (v != null && v != DBNull.Value && (oid & PqsqlDbType.Array) != PqsqlDbType.Array)
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

				mChanged = false;
			}

			return mPqPB;
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


		#region DbParameterCollection

		// Summary:
		//     Specifies the number of items in the collection.
		//
		// Returns:
		//     The number of items in the collection.
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public override int Count
		{
			get
			{
				return mParamList.Count;
			}
		}
		//
		// Summary:
		//     Specifies whether the collection is a fixed size.
		//
		// Returns:
		//     true if the collection is a fixed size; otherwise false.
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool IsFixedSize
		{
			get
			{
#if CODECONTRACTS
				Contract.Ensures(Contract.Result<bool>() == false);
#endif

				return false;
			}
		}
		//
		// Summary:
		//     Specifies whether the collection is read-only.
		//
		// Returns:
		//     true if the collection is read-only; otherwise false.
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool IsReadOnly
		{
			get
			{
#if CODECONTRACTS
				Contract.Ensures(Contract.Result<bool>() == false);
#endif

				return false;
			}
		}
		//
		// Summary:
		//     Specifies whether the collection is synchronized.
		//
		// Returns:
		//     true if the collection is synchronized; otherwise false.
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Browsable(false)]
		public override bool IsSynchronized
		{
			get
			{
				return (mParamList as ICollection).IsSynchronized;
			}
		}
		//
		// Summary:
		//     Specifies the System.Object to be used to synchronize access to the collection.
		//
		// Returns:
		//     A System.Object to be used to synchronize access to the System.Data.Common.DbParameterCollection.
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override object SyncRoot
		{
			get
			{
				return (mParamList as ICollection).SyncRoot;
			}
		}

		// Summary:
		//     Gets and sets the System.Data.Common.DbParameter at the specified index.
		//
		// Parameters:
		//   index:
		//     The zero-based index of the parameter.
		//
		// Returns:
		//     The System.Data.Common.DbParameter at the specified index.
		//
		// Exceptions:
		//   System.IndexOutOfRangeException:
		//     The specified index does not exist.
		public new PqsqlParameter this[int index]
		{
			get
			{
#if CODECONTRACTS
				Contract.Requires<IndexOutOfRangeException>(index >= 0);
#else
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index));
#endif

#if CODECONTRACTS
				Contract.Assume(index < mParamList.Count);
#endif

				return mParamList[index];
			}
			set
			{
#if CODECONTRACTS
				Contract.Requires<IndexOutOfRangeException>(index >= 0);
#else
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index));
#endif

#if CODECONTRACTS
				Contract.Assume(index < mParamList.Count);
#endif

				if (value == null)
				{
					throw new ArgumentNullException(nameof(value));	
				}

				PqsqlParameter old = mParamList[index];

				if (old == value)
				{
					return;
				}

				if (string.IsNullOrEmpty(value.ParameterName))
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				mParamList[index] = value;

				mLookup.Remove(old.ParameterName);

#if CODECONTRACTS
				Contract.Assert(value.ParameterName != null);
#endif

				mLookup.Add(value.ParameterName, index);

				mChanged = true;
			}
		}
		//
		// Summary:
		//     Gets and sets the System.Data.Common.DbParameter with the specified name.
		//
		// Parameters:
		//   parameterName:
		//     The name of the parameter.
		//
		// Returns:
		//     The System.Data.Common.DbParameter with the specified name.
		//
		// Exceptions:
		//   System.IndexOutOfRangeException:
		//     The specified index does not exist.
		public new PqsqlParameter this[string parameterName]
		{
			get
			{
				int i = IndexOf(parameterName);

				if (i == -1)
					throw new ArgumentOutOfRangeException(nameof(parameterName), "Parameter not found");

				return mParamList[i];
			}
			set
			{
				int i = IndexOf(parameterName);

				if (i == -1)
					throw new ArgumentOutOfRangeException(nameof(parameterName), "Parameter not found");

				mParamList[i] = value;
				mChanged = true;
			}
		}

		// Summary:
		//     Adds a System.Data.Common.DbParameter item with the specified value to the
		//     System.Data.Common.DbParameterCollection.
		//
		// Parameters:
		//   value:
		//     The System.Data.Common.DbParameter.Value of the System.Data.Common.DbParameter
		//     to add to the collection.
		//
		// Returns:
		//     The index of the System.Data.Common.DbParameter object in the collection.
		public override int Add(object value)
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<int>() >= -1);
#endif

			PqsqlParameter p = value as PqsqlParameter;
			
			if (p == null)
				return -1;

			int i;
			string s = p.ParameterName;

#if CODECONTRACTS
			Contract.Assume(s != null);
#endif

			if (!mLookup.TryGetValue(s, out i))
			{
				mParamList.Add(p);
				i = mParamList.Count - 1;
				mLookup.Add(s, i);
				mChanged = true;
			}

			return i < -1 ? -1 : i;
		}

		//
		// Summary:
		//     Adds an array of items with the specified values to the System.Data.Common.DbParameterCollection.
		//
		// Parameters:
		//   values:
		//     An array of values of type System.Data.Common.DbParameter to add to the collection.
		public override void AddRange(Array values)
		{
			Array.ForEach(values as PqsqlParameter[], val => Add(val));
		}

		//
		// Summary:
		//     Removes all System.Data.Common.DbParameter values from the System.Data.Common.DbParameterCollection.
		public override void Clear()
		{
			if (mPqPB != IntPtr.Zero)
				PqsqlBinaryFormat.pqpb_reset(mPqPB);
			mParamList.Clear();
			mLookup.Clear();
		}

		//
		// Summary:
		//     Indicates whether a System.Data.Common.DbParameter with the specified System.Data.Common.DbParameter.Value
		//     is contained in the collection.
		//
		// Parameters:
		//   value:
		//     The System.Data.Common.DbParameter.Value of the System.Data.Common.DbParameter
		//     to look for in the collection.
		//
		// Returns:
		//     true if the System.Data.Common.DbParameter is in the collection; otherwise
		//     false.
		public override bool Contains(object value)
		{
			return IndexOf(value) != -1;
		}
		//
		// Summary:
		//     Indicates whether a System.Data.Common.DbParameter with the specified name
		//     exists in the collection.
		//
		// Parameters:
		//   value:
		//     The name of the System.Data.Common.DbParameter to look for in the collection.
		//
		// Returns:
		//     true if the System.Data.Common.DbParameter is in the collection; otherwise
		//     false.
		public override bool Contains(string value)
		{
			return IndexOf(value) != -1;
		}
		//
		// Summary:
		//     Copies an array of items to the collection starting at the specified index.
		//
		// Parameters:
		//   array:
		//     The array of items to copy to the collection.
		//
		//   index:
		//     The index in the collection to copy the items.
		public override void CopyTo(Array array, int index)
		{
#if CODECONTRACTS
			Contract.Assume(index <= array.Length - mParamList.Count);
#endif
			(mParamList as ICollection).CopyTo(array, index);
		}
		//
		// Summary:
		//     Exposes the System.Collections.IEnumerable.GetEnumerator() method, which
		//     supports a simple iteration over a collection by a .NET Framework data provider.
		//
		// Returns:
		//     An System.Collections.IEnumerator that can be used to iterate through the
		//     collection.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override IEnumerator GetEnumerator()
		{
			return mParamList.GetEnumerator();
		}
		//
		// Summary:
		//     Returns the System.Data.Common.DbParameter object at the specified index
		//     in the collection.
		//
		// Parameters:
		//   index:
		//     The index of the System.Data.Common.DbParameter in the collection.
		//
		// Returns:
		//     The System.Data.Common.DbParameter object at the specified index in the collection.
		protected override DbParameter GetParameter(int i)
		{
#if CODECONTRACTS
			Contract.Assume(i >= 0 && i < Count);
#endif
			return this[i];
		}
		//
		// Summary:
		//     Returns System.Data.Common.DbParameter the object with the specified name.
		//
		// Parameters:
		//   parameterName:
		//     The name of the System.Data.Common.DbParameter in the collection.
		//
		// Returns:
		//     The System.Data.Common.DbParameter the object with the specified name.
		protected override DbParameter GetParameter(string parameterName)
		{
			int i = IndexOf(parameterName);

			if (i == -1)
			{
				throw new KeyNotFoundException("Could not find parameter name " + parameterName);
			}

			return this[i];
		}
		//
		// Summary:
		//     Returns the index of the specified System.Data.Common.DbParameter object.
		//
		// Parameters:
		//   value:
		//     The System.Data.Common.DbParameter object in the collection.
		//
		// Returns:
		//     The index of the specified System.Data.Common.DbParameter object.
		public override int IndexOf(object value)
		{
			return mParamList.IndexOf((PqsqlParameter) value);
		}

		//
		// Summary:
		//     Returns the index of the System.Data.Common.DbParameter object with the specified
		//     name.
		//
		// Parameters:
		//   parameterName:
		//     The name of the System.Data.Common.DbParameter object in the collection.
		//
		// Returns:
		//     The index of the System.Data.Common.DbParameter object with the specified
		//     name.
		public override int IndexOf(string parameterName)
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<int>() >= -1);
			Contract.Ensures(Contract.Result<int>() < Count);
#endif

			int ret;
			if (!string.IsNullOrEmpty(parameterName) &&
				mLookup.TryGetValue(PqsqlParameter.CanonicalParameterName(parameterName), out ret))
			{
				return ret < -1 ? -1 : ret;
			}

			return -1;
		}
		//
		// Summary:
		//     Inserts the specified index of the System.Data.Common.DbParameter object
		//     with the specified name into the collection at the specified index.
		//
		// Parameters:
		//   index:
		//     The index at which to insert the System.Data.Common.DbParameter object.
		//
		//   value:
		//     The System.Data.Common.DbParameter object to insert into the collection.
		public override void Insert(int index, object value)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(value != null);
#else
			if (value == null)
				throw new ArgumentNullException(nameof(value));
#endif

#if CODECONTRACTS
			Contract.Assert(index >= 0);
			Contract.Assume(index < mParamList.Count);
#endif

			PqsqlParameter val = value as PqsqlParameter;

			if (val == null)
				throw new InvalidCastException(nameof(value) + " is not a PqsqlParameter");

			string newParamName = val.ParameterName;

			if (newParamName == null)
				throw new ArgumentNullException(nameof(value), "ParameterName is null");

			if (mLookup.ContainsKey(newParamName))
				throw new DuplicateNameException("A key with name " + newParamName + " already exists in the collection");

			mParamList.Insert(index, val);
			
			// update lookup index
			foreach (KeyValuePair<string, int> kv in mLookup.Where(kv => kv.Value >= index).ToArray())
			{
				mLookup[kv.Key] = kv.Value + 1;
			}

			mLookup.Add(newParamName, index);

			mChanged = true;
		}
		//
		// Summary:
		//     Removes the specified System.Data.Common.DbParameter object from the collection.
		//
		// Parameters:
		//   value:
		//     The System.Data.Common.DbParameter object to remove.
		public override void Remove(object value)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(value != null);
#else
			if (value == null)
				throw new ArgumentNullException(nameof(value));
#endif

			int i = IndexOf(value);
			if (i >= 0)
			{
				RemoveAt(i);
				mChanged = true;
			}
		}
		//
		// Summary:
		//     Removes the System.Data.Common.DbParameter object at the specified from the
		//     collection.
		//
		// Parameters:
		//   index:
		//     The index where the System.Data.Common.DbParameter object is located.
		public override void RemoveAt(int index)
		{
#if CODECONTRACTS
			Contract.Assert(index >= 0);
			Contract.Assume(index < mParamList.Count);
#endif

			PqsqlParameter old = mParamList[index];

			if (old != null)
			{
				mParamList.RemoveAt(index);

				mLookup.Remove(old.ParameterName);

				// update lookup index
				foreach (KeyValuePair<string, int> kv in mLookup.Where(kv => kv.Value > index).ToArray())
				{
					mLookup[kv.Key] = kv.Value - 1;
				}

				mChanged = true;
			}
		}
		//
		// Summary:
		//     Removes the System.Data.Common.DbParameter object with the specified name
		//     from the collection.
		//
		// Parameters:
		//   parameterName:
		//     The name of the System.Data.Common.DbParameter object to remove.
		public override void RemoveAt(string parameterName)
		{
#if CODECONTRACTS
			Contract.Assume(parameterName != null);
#endif

			if (!string.IsNullOrEmpty(parameterName))
			{
				int ret;
				string canonical = PqsqlParameter.CanonicalParameterName(parameterName);

				if (mLookup.TryGetValue(canonical, out ret) && ret != -1)
				{
					mParamList.RemoveAt(ret);

					mLookup.Remove(canonical);
					
					// update lookup index
					foreach (KeyValuePair<string, int> kv in mLookup.Where(kv => kv.Value > ret).ToArray())
					{
						mLookup[kv.Key] = kv.Value - 1;
					}

					mChanged = true;
					return;
				}
			}

			throw new KeyNotFoundException("Could not find parameter name " + parameterName);
		}
		//
		// Summary:
		//     Sets the System.Data.Common.DbParameter object at the specified index to
		//     a new value.
		//
		// Parameters:
		//   index:
		//     The index where the System.Data.Common.DbParameter object is located.
		//
		//   value:
		//     The new System.Data.Common.DbParameter value.
		protected override void SetParameter(int index, DbParameter value)
		{
#if CODECONTRACTS
			Contract.Assume(index >= 0);
			Contract.Assume(index < Count);
#endif

			this[index] = (PqsqlParameter) value;
		}
		//
		// Summary:
		//     Sets the System.Data.Common.DbParameter object with the specified name to
		//     a new value.
		//
		// Parameters:
		//   parameterName:
		//     The name of the System.Data.Common.DbParameter object in the collection.
		//
		//   value:
		//     The new System.Data.Common.DbParameter value.
		protected override void SetParameter(string parameterName, DbParameter value)
		{
			this[parameterName] = (PqsqlParameter) value;
		}

		#endregion


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

			// we always interpret dt as Utc timestamp and ignore DateTime.Kind value
			long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
			long sec = ticks / TimeSpan.TicksPerSecond;
			int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);
			PqsqlBinaryFormat.pqbf_set_array_itemlength(a, 8);
			PqsqlBinaryFormat.pqbf_set_timestamp(a, sec, usec);
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

			// we always interpret dt as Utc timestamp and ignore DateTime.Kind value
			long ticks = dt.Ticks - PqsqlBinaryFormat.UnixEpochTicks;
			long sec = ticks / TimeSpan.TicksPerSecond;
			int usec = (int) (ticks % TimeSpan.TicksPerSecond / 10);
			PqsqlBinaryFormat.pqbf_add_timestamp(pb, sec, usec, (uint) oid);
		}

		#endregion
	}
}
