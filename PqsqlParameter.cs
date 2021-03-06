﻿using System;
using System.Data.Common;
using System.ComponentModel;
using System.Data;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

namespace Pqsql
{
	public sealed class PqsqlParameter : DbParameter
	{
		DbType mDbType;

		PqsqlDbType mPqsqlDbType;

		string mName;

		int mSize;

		object mValue;

		static readonly char[] mTrimStart = { ' ', ':', '\t', '\n' };

		// Summary:
		//     Initializes a new instance of the System.Data.Common.DbParameter class.
		public PqsqlParameter()
		{
			SourceColumn = String.Empty;
			Direction = ParameterDirection.Input;
			SourceVersion = DataRowVersion.Current;
			ResetDbType();
		}

		public PqsqlParameter(string parameterName, DbType parameterType)
			: this(parameterName, parameterType, 0, String.Empty)
		{
		}
		
		public PqsqlParameter(string parameterName, DbType parameterType, object value)
			: this(parameterName, parameterType, 0, String.Empty)
		{
			Value = value;
		}

		public PqsqlParameter(string parameterName, DbType parameterType, int size)
			: this(parameterName, parameterType, size, String.Empty)
		{
		}

		public PqsqlParameter(string parameterName, DbType parameterType, int size, string sourceColumn)
			: this()
		{
			ParameterName = parameterName;
			DbType = parameterType;
			mSize = size;
			SourceColumn = sourceColumn;
		}

		// creates canonical parameter name as lower-case string prefixed with ':'
		internal static string CanonicalParameterName(string parameterName)
		{
			if (string.IsNullOrEmpty(parameterName))
				return string.Empty;

			// always prefix with ':'
			if (parameterName[0] != '"')
				return ":" + parameterName.TrimStart(mTrimStart).TrimEnd().ToLowerInvariant();

			return ":" + parameterName;
		}

		// Summary:
		//     Gets or sets the System.Data.DbType of the parameter.
		//
		// Returns:
		//     One of the System.Data.DbType values. The default is System.Data.DbType.String.
		//
		// Exceptions:
		//   System.ArgumentException:
		//     The property is not set to a valid System.Data.DbType.
		[Browsable(false)]
		[RefreshProperties(RefreshProperties.All)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override DbType DbType
		{
			get
			{
				return mDbType;
			}
			set
			{
				mDbType = value;
				mPqsqlDbType = PqsqlTypeRegistry.GetPqsqlDbType(value);
			}
		}


		[Browsable(false)]
		[RefreshProperties(RefreshProperties.All)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PqsqlDbType PqsqlDbType
		{
			get
			{
				return mPqsqlDbType;
			}
			set
			{
				mPqsqlDbType = value;
				mDbType = PqsqlTypeRegistry.GetDbType(value & ~PqsqlDbType.Array);
			}
		}

		//
		// Summary:
		//     Gets or sets a value that indicates whether the parameter is input-only,
		//     output-only, bidirectional, or a stored procedure return value parameter.
		//
		// Returns:
		//     One of the System.Data.ParameterDirection values. The default is Input.
		//
		// Exceptions:
		//   System.ArgumentException:
		//     The property is not set to one of the valid System.Data.ParameterDirection
		//     values.
		[RefreshProperties(RefreshProperties.All)]
		public override ParameterDirection Direction
		{
			get;
			set;
		}

		//
		// Summary:
		//     Gets or sets a value that indicates whether the parameter accepts null values.
		//
		// Returns:
		//     true if null values are accepted; otherwise false. The default is false.
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DesignOnly(true)]
		public override bool IsNullable
		{
			get;
			set;
		}

		//
		// Summary:
		//     Gets or sets the name of the System.Data.Common.DbParameter.
		//
		// Returns:
		//     The name of the System.Data.Common.DbParameter. The default is an empty string
		//     ("").
		[DefaultValue("")]
		public override string ParameterName
		{
			get
			{
#if CODECONTRACTS
				Contract.Ensures(Contract.Result<System.String>() != null);
#endif

				return string.IsNullOrEmpty(mName) ? string.Empty : mName;
			}
			set
			{
				mName = CanonicalParameterName(value);
			}
		}

		//
		// Summary:
		//     Gets or sets the name of the System.Data.Common.DbParameter.
		//
		// Returns:
		//     The name of the System.Data.Common.DbParameter. The default is an empty string
		//     ("").
		[DefaultValue("")]
		internal string PsqlParameterName
		{
			get
			{
#if CODECONTRACTS
				Contract.Ensures(Contract.Result<System.String>() != null);
#endif

				return string.IsNullOrEmpty(mName) ? string.Empty : mName.Substring(1);
			}
		}

		//
		// Summary:
		//     Gets or sets the maximum size, in bytes, of the data within the column.
		//
		// Returns:
		//     The maximum size, in bytes, of the data within the column. The default value
		//     is inferred from the parameter value.
		public override int Size
		{
			get
			{
				return mSize;
			}
			set
			{
#if CODECONTRACTS
				Contract.Requires<ArgumentException>(value >= 0, "Invalid property Size value < 0");
#else
				if (value < 0)
					throw new ArgumentException("Invalid property Size value < 0");
#endif
				mSize = value;
			}
		}
		//
		// Summary:
		//     Gets or sets the name of the source column mapped to the System.Data.DataSet
		//     and used for loading or returning the System.Data.Common.DbParameter.Value.
		//
		// Returns:
		//     The name of the source column mapped to the System.Data.DataSet. The default
		//     is an empty string.
		[DefaultValue("")]
		public override String SourceColumn
		{
			get;
			set;
		}
		//
		// Summary:
		//     Sets or gets a value which indicates whether the source column is nullable.
		//     This allows System.Data.Common.DbCommandBuilder to correctly generate Update
		//     statements for nullable columns.
		//
		// Returns:
		//     true if the source column is nullable; false if it is not.
		[DefaultValue(false)]
		[RefreshProperties(RefreshProperties.All)]
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public override bool SourceColumnNullMapping
		{
			get;
			set;
		}
		//
		// Summary:
		//     Gets or sets the System.Data.DataRowVersion to use when you load System.Data.Common.DbParameter.Value.
		//
		// Returns:
		//     One of the System.Data.DataRowVersion values. The default is Current.
		//
		// Exceptions:
		//   System.ArgumentException:
		//     The property is not set to one of the System.Data.DataRowVersion values.
		[Category("Data"), DefaultValue(DataRowVersion.Current)]
		public override DataRowVersion SourceVersion
		{
			get;
			set;
		}
		//
		// Summary:
		//     Gets or sets the value of the parameter.
		//
		// Returns:
		//     An System.Object that is the value of the parameter. The default value is
		//     null.
		[RefreshProperties(RefreshProperties.All)]
		[DefaultValue("")]
		public override object Value
		{
			get
			{
				return mValue;
			}
			set
			{
				mValue = value;
			}
		}

		// Summary:
		//     Resets the DbType property to its original settings.
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public override void ResetDbType()
		{
			mDbType = DbType.Object;
			mPqsqlDbType = PqsqlDbType.Unknown;
			mValue = null;
		}
	}
}
