using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.ComponentModel;
using System.Data;

namespace Pqsql
{
	public class PqsqlCommand : DbCommand
	{
		protected string mCmdText;

		protected int mCmdTimeout;

		protected CommandType mCmdType;

		protected PqsqlConnection mConn;

		protected PqsqlParameterCollection mParams;

		protected PqsqlTransaction mTransaction;

		protected UpdateRowSource mUpdateRowSource = UpdateRowSource.Both;


		// Summary:
		//     Constructs an instance of the System.Data.Common.DbCommand object.
		public PqsqlCommand()
			: this("", null)
		{
		}

		public PqsqlCommand(PqsqlConnection conn)
			: this("", conn)
		{
		}

		public PqsqlCommand(string query, PqsqlConnection conn)
			: base()
		{
			Init(query);
			mConn = conn;
		}

		protected void Init(string q)
		{
			mCmdText = q;
			mCmdTimeout = 120;
			mCmdType = CommandType.Text;
		}

		// Summary:
		//     Gets or sets the text command to run against the data source.
		//
		// Returns:
		//     The text command to execute. The default value is an empty string ("").
		[RefreshProperties(RefreshProperties.All)]
		[DefaultValue("")]
		public override string CommandText
		{
			get
			{
				return mCmdText;
			}
			set
			{
				mCmdText = value;
			}
		}
		//
		// Summary:
		//     Gets or sets the wait time before terminating the attempt to execute a command
		//     and generating an error.
		//
		// Returns:
		//     The time in seconds to wait for the command to execute.
		public override int CommandTimeout
		{
			get	{	return mCmdTimeout; }
			set { mCmdTimeout = value; }
		}
		//
		// Summary:
		//     Indicates or specifies how the System.Data.Common.DbCommand.CommandText property
		//     is interpreted.
		//
		// Returns:
		//     One of the System.Data.CommandType values. The default is Text.
		[RefreshProperties(RefreshProperties.All)]
		public override CommandType CommandType
		{
			get	{	return mCmdType; }
			set {	mCmdType = value;	}
		}
		//
		// Summary:
		//     Gets or sets the System.Data.Common.DbConnection used by this System.Data.Common.DbCommand.
		//
		// Returns:
		//     The connection to the data source.
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new PqsqlConnection Connection
		{
			get	{	return mConn;	}
			set
			{
				if (value == null)
				{
					mConn = null;
				}
				else if (mConn != value)
				{
					mConn = value;
					value.Command = this;
				}
			}
		}

		// used by PqsqlConnection to get ConnectionState.Executing, ConnectionState.Fetching and
		// PqsqlDataReader sets ConnectionState.Executing, ConnectionState.Fetching
		public ConnectionState State
		{
			get;
			set;
		}


		//
		// Summary:
		//     Gets or sets the System.Data.Common.DbConnection used by this System.Data.Common.DbCommand.
		//
		// Returns:
		//     The connection to the data source.
		protected override DbConnection DbConnection
		{
			get
			{
				return Connection;
			}
			set
			{
				Connection = (PqsqlConnection) value;
			}
		}

		//
		// Summary:
		//     Gets the collection of System.Data.Common.DbParameter objects.
		//
		// Returns:
		//     The parameters of the SQL statement or stored procedure.
		protected override DbParameterCollection DbParameterCollection
		{
			get	{	return (DbParameterCollection) Parameters; }
		}

		//
		// Summary:
		//     Gets or sets the System.Data.Common.DbCommand.DbTransaction within which
		//     this System.Data.Common.DbCommand object executes.
		//
		// Returns:
		//     The transaction within which a Command object of a .NET Framework data provider
		//     executes. The default value is a null reference (Nothing in Visual Basic).
		protected override DbTransaction DbTransaction
		{
			get	{	return Transaction;	}
			set	{	Transaction = (PqsqlTransaction) value;	}
		}

		//
		// Summary:
		//     Gets or sets a value indicating whether the command object should be visible
		//     in a customized interface control.
		//
		// Returns:
		//     true, if the command object should be visible in a control; otherwise false.
		//     The default is true.
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DefaultValue(true)]
		[DesignOnly(true)]
		[Browsable(false)]
		public override bool DesignTimeVisible
		{
			get;
			set;
		}

		//
		// Summary:
		//     Gets the collection of System.Data.Common.DbParameter objects.
		//
		// Returns:
		//     The parameters of the SQL statement or stored procedure.
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public new PqsqlParameterCollection Parameters
		{
			get
			{
				return mParams;
			}
		}

		//
		// Summary:
		//     Gets or sets the System.Data.Common.DbTransaction within which this System.Data.Common.DbCommand
		//     object executes.
		//
		// Returns:
		//     The transaction within which a Command object of a .NET Framework data provider
		//     executes. The default value is a null reference (Nothing in Visual Basic).
		[Browsable(false)]
		[DefaultValue("")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new PqsqlTransaction Transaction
		{
			get
			{
				if (mTransaction != null && mTransaction.Connection == null)
				{
					mTransaction = null;
				}
				return mTransaction;
			}
			set
			{
				mTransaction = value;
			}
		}

		//
		// Summary:
		//     Gets or sets how command results are applied to the System.Data.DataRow when
		//     used by the Update method of a System.Data.Common.DbDataAdapter.
		//
		// Returns:
		//     One of the System.Data.UpdateRowSource values. The default is Both unless
		//     the command is automatically generated. Then the default is None.
		public override UpdateRowSource UpdatedRowSource
		{
			get
			{
				return mUpdateRowSource;
			}
			set
			{
				switch (value)
				{
					case UpdateRowSource.None:
					case UpdateRowSource.OutputParameters:
					case UpdateRowSource.FirstReturnedRecord:
					case UpdateRowSource.Both:
						mUpdateRowSource = value;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		// Summary:
		//     Attempts to cancels the execution of a System.Data.Common.DbCommand.
		public override void Cancel()
		{
			ConnectionState s = mConn.State;

			if ((s & (ConnectionState.Broken | ConnectionState.Connecting | ConnectionState.Closed)) > 0)
				return;

			IntPtr cancel = PqsqlWrapper.PQgetCancel(mConn.PGConnection);

			if (cancel != IntPtr.Zero)
			{
				sbyte[] buf = new sbyte[256];
				//string err = string.Empty;
				int cret;
				
				unsafe
				{
					fixed (sbyte* b = buf)
					{
						cret = PqsqlWrapper.PQcancel(cancel, b, 256);
						//if (cret == 0)
						//{
						//	err = new string(b, 0, 256);
						//}
					}
				}

				PqsqlWrapper.PQfreeCancel(cancel);

				//if (cret == 0)
				//{
				//	throw new PqsqlException("Could not cancel command: " + err);
				//}
			}
		}

		//
		// Summary:
		//     Creates a new instance of a System.Data.Common.DbParameter object.
		//
		// Returns:
		//     A System.Data.Common.DbParameter object.
		protected override DbParameter CreateDbParameter()
		{
			return new PqsqlParameter();
		}

		//
		// Summary:
		//     Creates a new instance of a System.Data.Common.DbParameter object.
		//
		// Returns:
		//     A System.Data.Common.DbParameter object.
		//public override DbParameter CreateParameter()
		//{
		//	return CreateDbParameter();
		//}
		//
		// Summary:
		//     Executes the command text against the connection.
		//
		// Parameters:
		//   behavior:
		//     An instance of System.Data.CommandBehavior.
		//
		// Returns:
		//     A System.Data.Common.DbDataReader.
		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			return ExecuteReader(behavior);
		}
		//
		// Summary:
		//     Executes a SQL statement against a connection object.
		//
		// Returns:
		//     The number of rows affected.
		public override int ExecuteNonQuery()
		{
			PqsqlDataReader r = ExecuteReader(CommandBehavior.Default);
			return r.RecordsAffected;
		}

		//
		// Summary:
		//     Executes the System.Data.Common.DbCommand.CommandText against the System.Data.Common.DbCommand.Connection,
		//     and returns an System.Data.Common.DbDataReader.
		//
		// Returns:
		//     A System.Data.Common.DbDataReader object.
		public new PqsqlDataReader ExecuteReader()
		{
			return ExecuteReader(CommandBehavior.Default);
		}

		//
		// Summary:
		//     Executes the System.Data.Common.DbCommand.CommandText against the System.Data.Common.DbCommand.Connection,
		//     and returns an System.Data.Common.DbDataReader using one of the System.Data.CommandBehavior
		//     values.
		//
		// Parameters:
		//   behavior:
		//     One of the System.Data.CommandBehavior values.
		//
		// Returns:
		//     An System.Data.Common.DbDataReader object.
		public new PqsqlDataReader ExecuteReader(CommandBehavior behavior)
		{
			string[] statements = null;

			switch(CommandType)
			{
				case CommandType.Text:
					statements = ParseStatements();
					break;

				case CommandType.StoredProcedure:
					StringBuilder sb = new StringBuilder();
					sb.Append("SELECT ");
					sb.Append(CommandText);
					sb.Append(" (");

					// add parameter index $i for each parameter
					int n = Parameters.Count;
					for (int i = 1; i <= n; i++)
 					{
						if (i > 1) sb.Append(',');
						sb.Append('$');
						sb.Append(i);
					}

					sb.Append(')');

					statements = new string[1];
					statements[0] = sb.ToString();
					break;

				case CommandType.TableDirect:
					statements = new string[1];
					statements[0] = "TABLE " + CommandText;
					break;
			}

			PqsqlDataReader reader = new PqsqlDataReader(this, behavior, statements);
			reader.Read(); // always fetch first row
			return reader;
		}

		//
		// Summary:
		//     Executes the query and returns the first column of the first row in the result
		//     set returned by the query. All other columns and rows are ignored.
		//
		// Returns:
		//     The first column of the first row in the result set.
		public override object ExecuteScalar()
		{
			PqsqlDataReader r = ExecuteReader(CommandBehavior.Default);
			return r.GetValue(0);
		}


		// replace parameter names :ParameterName with parameter index $j
		protected void ReplaceParameterNames(ref StringBuilder sb)
		{
			StringBuilder paramName = new StringBuilder();
			StringBuilder paramIndex = new StringBuilder();

			paramName.Append(':');
			paramIndex.Append('$');

			int n = mParams.Count;
			for (int i = 0; i < n; i++)
			{
				paramName.Append(mParams[i].ParameterName);
				paramIndex.Append(i + 1);
				sb.Replace(paramName.ToString(), paramIndex.ToString());
				paramName.Length = 1;
				paramIndex.Length = 1;
			}
		}

		/// <summary>
		/// split PqsqlCommand.CommandText into an array of sub-statements
		/// </summary>
		/// <returns></returns>
		protected string[] ParseStatements()
		{
			StringBuilder sb = new StringBuilder();
			bool quote = false;
			string[] statements = new string[0];
			int stmLen = 0; // length of statement

			// parse multiple statements separated by ;
			foreach (char c in CommandText.Trim())
			{
				if (quote) // eat input until next quote symbol (ignoring ';')
				{
					switch (c)
					{
						case '\'':
						case '"':
							quote = !quote;
							break;
					}
					sb.Append(c);
				}
				else
				{
					switch (c) // eat input until ;
					{
						case ';':
							Array.Resize(ref statements, stmLen + 1);
							ReplaceParameterNames(ref sb);
							statements[stmLen++] = sb.ToString();
							sb.Clear();
							continue;

						case '\'':
						case '"':
							quote = !quote;
							break;
					}
					sb.Append(c);
				}
			}

			if (sb.Length > 0) // add last statement not terminated by ';'
			{
				Array.Resize(ref statements, stmLen + 1);
				ReplaceParameterNames(ref sb);
				statements[stmLen] = sb.ToString();
				sb.Clear();
				sb = null;
			}

			return statements;
		}

		//
		// Summary:
		//     Creates a prepared (or compiled) version of the command on the data source.
		public override void Prepare()
		{
			throw new NotImplementedException("Prepare() is not implemented");
		}
	}
}
