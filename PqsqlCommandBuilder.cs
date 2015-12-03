﻿using System;
using System.Data.Common;
using System.Data;
using System.Globalization;

namespace Pqsql
{
	public sealed class PqsqlCommandBuilder : DbCommandBuilder
	{

		// Summary:
		//     Initializes a new instance of a class that inherits from the System.Data.Common.DbCommandBuilder
		//     class.
		public PqsqlCommandBuilder()
			: this(null)
		{
		}

		public PqsqlCommandBuilder(PqsqlDataAdapter adapter)
		{
			DataAdapter = adapter;
			QuotePrefix = "\"";
			QuoteSuffix = "\"";
		}

		// Summary:
		//     Sets or gets the System.Data.Common.CatalogLocation for an instance of the
		//     System.Data.Common.DbCommandBuilder class.
		//
		// Returns:
		//     A System.Data.Common.CatalogLocation object.
	//	public virtual CatalogLocation CatalogLocation
	//	{
	//		get;
	//		set;
	//	}
		//
		// Summary:
		//     Sets or gets a string used as the catalog separator for an instance of the
		//     System.Data.Common.DbCommandBuilder class.
		//
		// Returns:
		//     A string indicating the catalog separator for use with an instance of the
		//     System.Data.Common.DbCommandBuilder class.
	//	[DefaultValue(".")]
	//	public virtual string CatalogSeparator
	//	{
	//		get;
	//		set;
	//	}
		//
		// Summary:
		//     Specifies which System.Data.ConflictOption is to be used by the System.Data.Common.DbCommandBuilder.
		//
		// Returns:
		//     Returns one of the System.Data.ConflictOption values describing the behavior
		//     of this System.Data.Common.DbCommandBuilder.
	//	public virtual ConflictOption ConflictOption
	//	{
	//		get;
	//		set;
	//	}
		//
		// Summary:
		//     Gets or sets a System.Data.Common.DbDataAdapter object for which Transact-SQL
		//     statements are automatically generated.
		//
		// Returns:
		//     A System.Data.Common.DbDataAdapter object.
	//	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	//	[Browsable(false)]
	//	public DbDataAdapter DataAdapter
	//	{
	//		get;
	//		set;
	//	}
		//
		// Summary:
		//     Gets or sets the beginning character or characters to use when specifying
		//     database objects (for example, tables or columns) whose names contain characters
		//     such as spaces or reserved tokens.
		//
		// Returns:
		//     The beginning character or characters to use. The default is an empty string.
		//
		// Exceptions:
		//   System.InvalidOperationException:
		//     This property cannot be changed after an insert, update, or delete command
		//     has been generated.
		public override string QuotePrefix
		{
			get
			{
				return base.QuotePrefix;
			}
			// TODO: Why should it be possible to remove the QuotePrefix?
			set
			{
				if (String.IsNullOrEmpty(value))
				{
					base.QuotePrefix = value;
				}
				else
				{
					base.QuotePrefix = "\"";
				}
			}
		}

		//
		// Summary:
		//     Gets or sets the ending character or characters to use when specifying database
		//     objects (for example, tables or columns) whose names contain characters such
		//     as spaces or reserved tokens.
		//
		// Returns:
		//     The ending character or characters to use. The default is an empty string.
		public override string QuoteSuffix
		{
			get
			{
				return base.QuoteSuffix;
			}
			// TODO: Why should it be possible to remove the QuoteSuffix?
			set
			{
				if (String.IsNullOrEmpty(value))
				{
					base.QuoteSuffix = value;
				}
				else
				{
					base.QuoteSuffix = "\"";
				}
			}
		}

		//
		// Summary:
		//     Specifies whether all column values in an update statement are included or
		//     only changed ones.
		//
		// Returns:
		//     true if the UPDATE statement generated by the System.Data.Common.DbCommandBuilder
		//     includes all columns; false if it includes only changed columns.
	//	[DefaultValue(false)]
	//	public bool SetAllValues
	//	{
	//		get;
	//		set;
	//	}

		// Summary:
		//     Allows the provider implementation of the System.Data.Common.DbCommandBuilder
		//     class to handle additional parameter properties.
		//
		// Parameters:
		//   parameter:
		//     A System.Data.Common.DbParameter to which the additional modifications are
		//     applied.
		//
		//   row:
		//     The System.Data.DataRow from the schema table provided by System.Data.Common.DbDataReader.GetSchemaTable().
		//
		//   statementType:
		//     The type of command being generated; INSERT, UPDATE or DELETE.
		//
		//   whereClause:
		//     true if the parameter is part of the update or delete WHERE clause, false
		//     if it is part of the insert or update values.
		protected override void ApplyParameterInfo(DbParameter p, DataRow row, StatementType statementType, bool whereClause)
		{
			// TODO: We may need to set DbType, as well as other properties, on p
			throw new NotImplementedException("ApplyParameterInfo");
		}
		//
		// Summary:
		//     Releases the unmanaged resources used by the System.Data.Common.DbCommandBuilder
		//     and optionally releases the managed resources.
		//
		// Parameters:
		//   disposing:
		//     true to release both managed and unmanaged resources; false to release only
		//     unmanaged resources.
		//protected override void Dispose(bool disposing);
		//
		// Summary:
		//     Gets the automatically generated System.Data.Common.DbCommand object required
		//     to perform deletions at the data source.
		//
		// Returns:
		//     The automatically generated System.Data.Common.DbCommand object required
		//     to perform deletions.
		public new PqsqlCommand GetDeleteCommand()
		{
			return GetDeleteCommand(false);
		}
		//
		// Summary:
		//     Gets the automatically generated System.Data.Common.DbCommand object required
		//     to perform deletions at the data source, optionally using columns for parameter
		//     names.
		//
		// Parameters:
		//   useColumnsForParameterNames:
		//     If true, generate parameter names matching column names, if possible. If
		//     false, generate @p1, @p2, and so on.
		//
		// Returns:
		//     The automatically generated System.Data.Common.DbCommand object required
		//     to perform deletions.
		public new PqsqlCommand GetDeleteCommand(bool useColumnsForParameterNames)
		{
			PqsqlCommand cmd = (PqsqlCommand) base.GetDeleteCommand(useColumnsForParameterNames);
			cmd.UpdatedRowSource = UpdateRowSource.None;
			return cmd;
		}
		//
		// Summary:
		//     Gets the automatically generated System.Data.Common.DbCommand object required
		//     to perform insertions at the data source.
		//
		// Returns:
		//     The automatically generated System.Data.Common.DbCommand object required
		//     to perform insertions.
		public new PqsqlCommand GetInsertCommand()
		{
			return GetInsertCommand(false);
		}
		//
		// Summary:
		//     Gets the automatically generated System.Data.Common.DbCommand object required
		//     to perform insertions at the data source, optionally using columns for parameter
		//     names.
		//
		// Parameters:
		//   useColumnsForParameterNames:
		//     If true, generate parameter names matching column names, if possible. If
		//     false, generate @p1, @p2, and so on.
		//
		// Returns:
		//     The automatically generated System.Data.Common.DbCommand object required
		//     to perform insertions.
		public new PqsqlCommand GetInsertCommand(bool useColumnsForParameterNames)
		{
			PqsqlCommand cmd = (PqsqlCommand) base.GetInsertCommand(useColumnsForParameterNames);
			cmd.UpdatedRowSource = UpdateRowSource.None;
			return cmd;
		}
		//
		// Summary:
		//     Returns the name of the specified parameter in the format of @p#. Use when
		//     building a custom command builder.
		//
		// Parameters:
		//   parameterOrdinal:
		//     The number to be included as part of the parameter's name..
		//
		// Returns:
		//     The name of the parameter with the specified number appended as part of the
		//     parameter name.
		protected override string GetParameterName(int parameterOrdinal)
		{
			return String.Format(CultureInfo.InvariantCulture, "@p{0}", parameterOrdinal);
		}
		//
		// Summary:
		//     Returns the full parameter name, given the partial parameter name.
		//
		// Parameters:
		//   parameterName:
		//     The partial name of the parameter.
		//
		// Returns:
		//     The full parameter name corresponding to the partial parameter name requested.
		protected override string GetParameterName(string parameterName)
		{
			return String.Format(CultureInfo.InvariantCulture, "@{0}", parameterName);
		}
		//
		// Summary:
		//     Returns the placeholder for the parameter in the associated SQL statement.
		//
		// Parameters:
		//   parameterOrdinal:
		//     The number to be included as part of the parameter's name.
		//
		// Returns:
		//     The name of the parameter with the specified number appended.
		protected override string GetParameterPlaceholder(int parameterOrdinal)
		{
			return GetParameterName(parameterOrdinal);
		}
		//
		// Summary:
		//     Gets the automatically generated System.Data.Common.DbCommand object required
		//     to perform updates at the data source.
		//
		// Returns:
		//     The automatically generated System.Data.Common.DbCommand object required
		//     to perform updates.
		public new PqsqlCommand GetUpdateCommand()
		{
			return GetUpdateCommand(false);
		}
		//
		// Summary:
		//     Gets the automatically generated System.Data.Common.DbCommand object required
		//     to perform updates at the data source, optionally using columns for parameter
		//     names.
		//
		// Parameters:
		//   useColumnsForParameterNames:
		//     If true, generate parameter names matching column names, if possible. If
		//     false, generate @p1, @p2, and so on.
		//
		// Returns:
		//     The automatically generated System.Data.Common.DbCommand object required
		//     to perform updates.
		public new PqsqlCommand GetUpdateCommand(bool useColumnsForParameterNames)
		{
			PqsqlCommand cmd = (PqsqlCommand) base.GetUpdateCommand(useColumnsForParameterNames);
			cmd.UpdatedRowSource = UpdateRowSource.None;
			return cmd;
		}
		//
		// Summary:
		//     Resets the System.Data.Common.DbCommand.CommandTimeout, System.Data.Common.DbCommand.Transaction,
		//     System.Data.Common.DbCommand.CommandType, and System.Data.UpdateRowSource
		//     properties on the System.Data.Common.DbCommand.
		//
		// Parameters:
		//   command:
		//     The System.Data.Common.DbCommand to be used by the command builder for the
		//     corresponding insert, update, or delete command.
		//
		// Returns:
		//     A System.Data.Common.DbCommand instance to use for each insert, update, or
		//     delete operation. Passing a null value allows the System.Data.Common.DbCommandBuilder.InitializeCommand(System.Data.Common.DbCommand)
		//     method to create a System.Data.Common.DbCommand object based on the Select
		//     command associated with the System.Data.Common.DbCommandBuilder.
		//protected virtual DbCommand InitializeCommand(DbCommand command);
		//
		// Summary:
		//     Given an unquoted identifier in the correct catalog case, returns the correct
		//     quoted form of that identifier, including properly escaping any embedded
		//     quotes in the identifier.
		//
		// Parameters:
		//   unquotedIdentifier:
		//     The original unquoted identifier.
		//
		// Returns:
		//     The quoted version of the identifier. Embedded quotes within the identifier
		//     are properly escaped.
		public override string QuoteIdentifier(string unquotedIdentifier)
		{
			if (unquotedIdentifier == null)
			{
				throw new ArgumentNullException("unquotedIdentifier", "Unquoted identifier must not be null");
			}

			return String.Format("{0}{1}{2}", QuotePrefix, unquotedIdentifier.Replace(QuotePrefix, QuotePrefix + QuotePrefix), QuoteSuffix);
		}
		//
		// Summary:
		//     Clears the commands associated with this System.Data.Common.DbCommandBuilder.
		//public virtual void RefreshSchema();


		//
		// Summary:
		//     Adds an event handler for the System.Data.OleDb.OleDbDataAdapter.RowUpdating
		//     event.
		//
		// Parameters:
		//   rowUpdatingEvent:
		//     A System.Data.Common.RowUpdatingEventArgs instance containing information
		//     about the event.
		//protected void RowUpdatingHandler(RowUpdatingEventArgs rowUpdatingEvent);


		//
		// Summary:
		//     Registers the System.Data.Common.DbCommandBuilder to handle the System.Data.OleDb.OleDbDataAdapter.RowUpdating
		//     event for a System.Data.Common.DbDataAdapter.
		//
		// Parameters:
		//   adapter:
		//     The System.Data.Common.DbDataAdapter to be used for the update.
		protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
		{
			PqsqlDataAdapter pa = adapter as PqsqlDataAdapter;
			if (pa == null)
				throw new ArgumentException("adapter needs to be a PqsqlDataAdapter");

			PqsqlRowUpdatingEventHandler handler = (s, e) => RowUpdatingHandler(e);

			// unregister if we had registered adapter before
			if (adapter == DataAdapter)
				pa.RowUpdating -= handler;
			else
				pa.RowUpdating += handler;
		}

		//
		// Summary:
		//     Given a quoted identifier, returns the correct unquoted form of that identifier,
		//     including properly un-escaping any embedded quotes in the identifier.
		//
		// Parameters:
		//   quotedIdentifier:
		//     The identifier that will have its embedded quotes removed.
		//
		// Returns:
		//     The unquoted identifier, with embedded quotes properly un-escaped.
		public override string UnquoteIdentifier(string quotedIdentifier)
		{
			if (quotedIdentifier == null)
			{
				throw new ArgumentNullException("quotedIdentifier", "Quoted identifier parameter cannot be null");
			}

			string uqid = quotedIdentifier.Trim().Replace(QuotePrefix + QuotePrefix, QuotePrefix);

			int beg = 0;
			int len = uqid.Length;

			if (uqid.StartsWith(QuotePrefix))
			{
				beg++;
			}

			if (uqid.EndsWith(QuoteSuffix))
			{
				len = uqid.Length - beg - 1;
			}

			return uqid.Substring(beg, len);
		}
	}
}
