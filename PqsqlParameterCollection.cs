using System;
using System.Collections.Generic;
using System.Data.Common;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Linq;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

namespace Pqsql
{
	public sealed class PqsqlParameterCollection : DbParameterCollection
	{
		// input and output parameters
		private readonly List<PqsqlParameter> mParamList = new List<PqsqlParameter>();

		// Dictionary lookups for GetValue to improve performance
		private readonly Dictionary<string, int> mLookup = new Dictionary<string, int>();

		// internal constructor
		internal PqsqlParameterCollection()
		{
		}

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

				this[i] = value;
			}
		}

		// add PqsqlParameter with value to PqsqlParameterCollection without known DbType
		public PqsqlParameter AddWithValue(string parameterName, object value)
		{
			if (string.IsNullOrEmpty(parameterName))
				throw new ArgumentOutOfRangeException(nameof(parameterName), "parameter name is empty or null");

			int i = IndexOf(parameterName);

			PqsqlParameter p;

			if (i >= 0)
			{
				// re-use PqsqlParameter
				p = this[i];

				// reset all properties
				p.ResetDbType();
				p.SourceColumn = string.Empty;
				p.Size = 0;
				p.Direction = ParameterDirection.Input;
				p.SourceVersion = DataRowVersion.Current;
				p.IsNullable = false;

				// set name and value
				p.ParameterName = parameterName;
				p.Value = value;
			}
			else
			{
				// fresh PqsqlParameter with name and value
				p = new PqsqlParameter
				{
					ParameterName = parameterName,
					Value = value
				};

				mParamList.Add(p);
				i = mParamList.Count - 1;
				mLookup.Add(p.ParameterName, i);
			}

			return p;
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

	}
}
