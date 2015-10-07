﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.ComponentModel;
using System.Collections;

namespace Pqsql
{
	public class PqsqlParameterCollection : DbParameterCollection, IDisposable
	{
		private readonly List<PqsqlParameter> mParamList = new List<PqsqlParameter>();

		// Dictionary lookups for GetValue to improve performance
		private Dictionary<string, int> lookup;

		// TODO release memory
		private IntPtr mPqPB;

		// Summary:
		//     Initializes a new instance of the System.Data.Common.DbParameterCollection
		//     class.
		public PqsqlParameterCollection()
		{
			Init();
		}

		protected void Init()
		{
			mPqPB = PqsqlBinaryFormat.pqpb_create(); // create pqparam_buffer
			lookup = new Dictionary<string, int>();
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

		bool mDisposed = false;

		protected void Dispose(bool disposing)
		{
			if (mDisposed)
			{
				return;
			}

			PqsqlBinaryFormat.pqpb_free(mPqPB);
			mDisposed = true;
		}



		#region pqparam_buffer*

		internal IntPtr PGParameters
		{
			get
			{
				if (PqsqlBinaryFormat.pqpb_get_num(mPqPB) != mParamList.Count)
				{
					PqsqlBinaryFormat.pqpb_reset(mPqPB);

					foreach (PqsqlParameter p in mParamList)
					{
						PqsqlDbType oid = p.PqsqlDbType;

						if (p.IsNullable && p.Value == null)
						{
							PqsqlBinaryFormat.pqbf_set_null(mPqPB, (uint) oid);
						}
						else
						{
							PqsqlTypeNames.SetValue(oid)(mPqPB, p.Value);
						}
					}
				}
				
				return mPqPB;
			}
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
				return mParamList[index];
			}
			set
			{
				PqsqlParameter old = mParamList[index];

				if (old == value)
				{
					return;
				}

				mParamList[index] = value;

				lookup.Remove(old.ParameterName);
				lookup.Add(value.ParameterName, index);
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
				{
					throw new IndexOutOfRangeException("Parameter not found");
				}

				return mParamList[i];
			}
			set
			{
				int i = IndexOf(parameterName);

				if (i == -1)
				{
					throw new IndexOutOfRangeException("Parameter not found");
				}

				mParamList[i] = value;
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
			PqsqlParameter p = value as PqsqlParameter;
			
			if (p == null)
				return -1;

			int i;
			string s = p.ParameterName;

			if (!lookup.TryGetValue(s, out i))
			{
				mParamList.Add(p);
				i = mParamList.Count - 1;
				lookup.Add(s, i);
			}

			return i;
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
			Array.ForEach<PqsqlParameter>(values as PqsqlParameter[], val => Add(val));
		}

		//
		// Summary:
		//     Removes all System.Data.Common.DbParameter values from the System.Data.Common.DbParameterCollection.
		public override void Clear()
		{
			if (mPqPB != IntPtr.Zero)
				PqsqlBinaryFormat.pqpb_reset(mPqPB);
			mParamList.Clear();
			lookup.Clear();
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
			return (IndexOf(value) != -1);
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
			return (IndexOf(value) != -1);
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
		public override int IndexOf(string param)
		{
			if (param == null)
			{
				return -1;
			}

			if (param.Length > 0 && ((param[0] == ':') || (param[0] == '@')))
			{
				param = param.Remove(0, 1);
			}

			param = param.Trim().ToLowerInvariant();

			int ret;
			if (lookup.TryGetValue(param, out ret))
			{
				return ret;
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
			if (value == null)
			{
				throw new ArgumentNullException("value must not be null");
			}

			PqsqlParameter val = value as PqsqlParameter;

			PqsqlParameter old = mParamList[index];
			if (old != null)
			{
				lookup.Remove(old.ParameterName);
			}

			mParamList.Insert(index, val);
			lookup.Add(val.ParameterName, index);
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
			if (value == null)
			{
				throw new ArgumentNullException("value must not be null");
			}

			RemoveAt(IndexOf(value));
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
			if (index < 0 || index >= mParamList.Count)
			{
				throw new ArgumentOutOfRangeException("index out of range");
			}

			PqsqlParameter old = mParamList[index];

			if (old != null)
			{
				lookup.Remove(old.ParameterName);
				mParamList.RemoveAt(index);
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
			int i = IndexOf(parameterName);

			if (i != -1)
			{
				lookup.Remove(parameterName);
				mParamList.RemoveAt(i);
			}
			else
			{
				throw new KeyNotFoundException("Could not find parameter name " + parameterName);
			}
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
			this[index] = (PqsqlParameter)value;
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
