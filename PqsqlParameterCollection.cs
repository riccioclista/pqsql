using System;
using System.Collections.Generic;
using System.Data.Common;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Globalization;
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
		}

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



		#region pqparam_buffer* setup

		/// <summary>
		/// create parameter buffer for statement input parameters.
		/// we convert and infer the right data type in case the user supplied inconsistent type information.
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

					PqsqlTypeRegistry.PqsqlTypeName tn = PqsqlTypeRegistry.Get(oid & ~PqsqlDbType.Array);
					if (tn == null)
					{
						// do not try to fetch datatype specs with PqsqlTypeRegistry.FetchType() here, just bail out
						throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Datatype {0} is not supported", oid & ~PqsqlDbType.Array));
					}

					// try to convert to the proper datatype in case the user supplied a wrong PqsqlDbType
					if (v != null && v != DBNull.Value && (oid & PqsqlDbType.Array) != PqsqlDbType.Array)
					{
						TypeCode tc = tn.TypeCode;

						if (vtc != TypeCode.Empty && vtc != tc)
						{
							v = ConvertParameterValue(v, vtc, tc, oid);
						}
					}

					// add parameter to the parameter buffer
					AddParameterValue(tn, oid, v);
				}

				mChanged = false;
			}

			return mPqPB;
		}

		// dispatch parameter type and add it to parameter buffer mPqPB
		private void AddParameterValue(PqsqlTypeRegistry.PqsqlTypeName tn, PqsqlDbType oid, object v)
		{
			if (v == null || v == DBNull.Value)
			{
				// null arrays must have oid of element type
				PqsqlBinaryFormat.pqbf_add_null(mPqPB, (uint) (oid & ~PqsqlDbType.Array));
			}
			else if ((oid & PqsqlDbType.Array) == PqsqlDbType.Array)
			{
				PqsqlTypeRegistry.SetArrayValue(oid, tn)(mPqPB, v);
			}
			else
			{
#if CODECONTRACTS
				Contract.Assume(tn.SetValue != null);
#endif

				tn.SetValue(mPqPB, v, oid);
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

				mParamList[index] = value;

				mLookup.Remove(old.ParameterName);

#if CODECONTRACTS
				Contract.Assume(value.ParameterName != null);
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
			if (val.ParameterName == null)
				throw new ArgumentNullException(nameof(value), "ParameterName is null");

			PqsqlParameter old = mParamList[index];
			if (old?.ParameterName != null)
			{
				mLookup.Remove(old.ParameterName);
			}

			mParamList.Insert(index, val);
			mLookup.Add(val.ParameterName, index);
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
				mLookup.Remove(old.ParameterName);
				mParamList.RemoveAt(index);
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
					mLookup.Remove(canonical);
					mParamList.RemoveAt(ret);
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
	}
}
