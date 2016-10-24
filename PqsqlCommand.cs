using System;
using System.Text;
using System.Data.Common;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Contracts;

namespace Pqsql
{
	public sealed class PqsqlCommand : DbCommand
	{
		const string mStatementTimeoutString = "statement_timeout";
		const string mApplicationNameString = "application_name";
		const string mStoredProcString = "select * from ";
		const string mTableString = "table ";

		private string mCmdText;

		private int mCmdTimeout;

		private bool mCmdTimeoutSet;

		private CommandType mCmdType;

		private PqsqlConnection mConn;

		private readonly PqsqlParameterCollection mParams;

		private PqsqlTransaction mTransaction;

		private UpdateRowSource mUpdateRowSource = UpdateRowSource.Both;


		[ContractInvariantMethod]
		private void ClassInvariant()
		{
			Contract.Invariant(mParams != null);
		}


		// Summary:
		//     Constructs an instance of the System.Data.Common.DbCommand object.
		public PqsqlCommand()
			: this(string.Empty, null)
		{
		}

		public PqsqlCommand(PqsqlConnection conn)
			: this(string.Empty, conn)
		{
		}

		public PqsqlCommand(string query, PqsqlConnection conn)
		{
			Init(query);
			mParams = new PqsqlParameterCollection();
			mConn = conn;
		}

		private void Init(string q)
		{
			mCmdText = q;
			mCmdTimeout = -1;
			mCmdTimeoutSet = false;
			mCmdType = CommandType.Text;
		}

		~PqsqlCommand()
		{
			Dispose(false);
		}

		#region Overrides of Component

		public new void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private bool mDisposed;

		protected override void Dispose(bool disposing)
		{
			if (mDisposed)
			{
				return;
			}

			if (disposing)
			{
				mParams.Dispose();
				mTransaction = null;
				mConn = null;
			}

			base.Dispose(disposing);
			mDisposed = true;
		}

		#endregion

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
				if (value == null)
					throw new ArgumentNullException("CommandText");
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
			set
			{
				int newTimeout = value*1000;
				if (mCmdTimeout != newTimeout)
				{
					mCmdTimeout = newTimeout;
					mCmdTimeoutSet = false;
				}
			}
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
				// ReSharper disable once RedundantCheckBeforeAssignment
				else if (mConn != value)
				{
					mConn = value;
				}
			}
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
			get	{	return Parameters; }
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
				Contract.Ensures(Contract.Result<PqsqlParameterCollection>() != null);
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
			if (mConn == null)
				return;

			ConnectionState s = mConn.State;

			// no cancel possible/necessary if connection is closed / open / connecting / broken
			if (s == ConnectionState.Closed || s == ConnectionState.Open || (s & (ConnectionState.Broken | ConnectionState.Connecting)) > 0)
				return;

			IntPtr cancel = PqsqlWrapper.PQgetCancel(mConn.PGConnection);

			if (cancel != IntPtr.Zero)
			{
				sbyte[] buf = new sbyte[256];

				string err;
				unsafe
				{
					fixed (sbyte* b = buf)
					{
						int cret = PqsqlWrapper.PQcancel(cancel, b, 256);
						PqsqlWrapper.PQfreeCancel(cancel);

						if (cret == 1)
							return;

						err = PqsqlUTF8Statement.CreateStringFromUTF8(new IntPtr(b));
					}
				}

				throw new PqsqlException("Could not cancel command «" + mCmdText + "»: " + err);
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
			return CreateParameter();
		}

		//
		// Summary:
		//     Creates a new instance of a System.Data.Common.DbParameter object.
		//
		// Returns:
		//     A System.Data.Common.DbParameter object.
		public new PqsqlParameter CreateParameter()
		{
			Contract.Ensures(Contract.Result<PqsqlParameter>() != null);
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

			// fill OUT and INOUT parameters with result tuple from the first row
			if (CommandType == CommandType.StoredProcedure)
			{
				r.Read(); // reading the first row will fill output parameters
			}

			int ra = r.RecordsAffected;
			r.Consume(); // sync protocol: consume remaining rows
			return ra;
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
			Contract.Ensures(Contract.Result<PqsqlDataReader>() != null);
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
			Contract.Ensures(Contract.Result<PqsqlDataReader>() != null);

			string[] statements;

#if false
			// always set SingleRow mode for now,
			// will be turned off for FETCH statements in PqsqlDataReader.Execute()
			behavior |= CommandBehavior.SingleRow;
#endif

			switch(CommandType)
			{
				case CommandType.Text:
					statements = ParseStatements();
					break;

				case CommandType.StoredProcedure:
					statements = BuildStoredProcStatement();
					break;

				case CommandType.TableDirect:
					statements = BuildTableStatement();
					break;

				default:
					throw new InvalidEnumArgumentException("unknown CommandType");
			}

			Contract.Assert(statements != null);

			if (statements.Length < 2)
				behavior |= CommandBehavior.SingleResult;

			CheckOpen();

			Contract.Assert(mConn != null);

			SetStatementTimeout();

			SetApplicationName();

			PqsqlDataReader reader = new PqsqlDataReader(this, behavior, statements);
			reader.NextResult(); // always execute first command
			return reader;
		}

		private string[] BuildTableStatement()
		{
			Contract.Ensures(Contract.Result<string[]>() != null);

			string[] statements = new string[1];

			StringBuilder tq = new StringBuilder(mTableString);
			tq.Append(CommandText);

			statements[0] = tq.ToString();

			return statements;
		}

		private string[] BuildStoredProcStatement()
		{
			Contract.Ensures(Contract.Result<string[]>() != null);

			string[] statements = new string[1];

			StringBuilder spq = new StringBuilder();
			spq.Append(mStoredProcString);
			spq.Append(CommandText);
			spq.Append('(');

			// add parameter index $i for each IN and INOUT parameter
			int i = 1;
			bool comma = false;
			foreach (PqsqlParameter p in Parameters)
			{
				if (p.Direction == ParameterDirection.Output) // skip output parameters
					continue;
				
				if (comma)
				{
					spq.Append(',');
				}
				spq.Append('$');
				spq.Append(i++);
				comma = true;			
			}

			spq.Append(')');

			statements[0] = spq.ToString();

			return statements;
		}

		// open connection if it is closed or broken
		private void CheckOpen()
		{
			Contract.Assume(mConn != null);

			ConnectionState s = mConn.State;

			if (s == ConnectionState.Closed || (s & ConnectionState.Broken) > 0)
			{
				mConn.Open();
			}
		}

		// executes SET parameter=value
		private void SetSessionParameter(string parameter, object value, bool quote)
		{
			Contract.Assume(mConn != null);

			StringBuilder sb = new StringBuilder();
			sb.Append("set ");
			sb.Append(parameter);
			sb.Append('=');

			if (quote) sb.Append('"');
			sb.Append(value);
			if (quote) sb.Append('"');

			byte[] stmt = PqsqlUTF8Statement.CreateUTF8Statement(sb);
			ExecStatusType s = mConn.Exec(stmt);

			if (s != ExecStatusType.PGRES_COMMAND_OK)
			{
				string err = mConn.GetErrorMessage();
				throw new PqsqlException("Could not set " + parameter + " to «" + value + "»: " + err);
			}
		}

		// sets application_name of the current session
		private void SetApplicationName()
		{
			Contract.Assume(mConn != null);
			string appname = mConn.ApplicationName;
			if (!string.IsNullOrEmpty(appname))
			{
				SetSessionParameter(mApplicationNameString, appname, true);
			}
		}

		// sets statement_timeout of the current session
		private void SetStatementTimeout()
		{
			if (mCmdTimeout > 0 && mCmdTimeoutSet == false)
			{
				SetSessionParameter(mStatementTimeoutString, CommandTimeout, false);
			}
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

			object o;
			if (r.Read())
				o = r.GetValue(0);
			else
				o = null;

			r.Consume(); // sync protocol: consume remaining rows
			return o;
		}


		#region parse sql statements and replace parameter names

		// statement parser states
		const int QUOTE = (1 << 0);
		const int DOLLARQUOTE = (1 << 1);
		const int DOLLARQUOTE0 = (1 << 2);
		const int DOLLARQUOTE1 = (1 << 3);
		const int PARAM0 = (1 << 4);
		const int PARAM1 = (1 << 5);
		const int ESCAPE = (1 << 6);


		// resize statements to i+1, and set i to sb
		private void ResizeAndSetStatements(ref StringBuilder statement, ref string[] statements, int i)
		{
			Contract.Requires<ArgumentNullException>(statements != null);
			Contract.Requires<ArgumentNullException>(statement != null);
			Contract.Requires<ArgumentNullException>(i >= 0);

			Array.Resize(ref statements, i + 1);
			statements[i] = statement.ToString().TrimStart();
			statement.Clear();
		}

		// replace parameter name with $ index in statement
		private void ReplaceParameter(ref StringBuilder statement, ref StringBuilder paramName, ref StringBuilder paramIndex)
		{
			Contract.Requires<ArgumentNullException>(statement != null);
			Contract.Requires<ArgumentNullException>(paramName != null);
			Contract.Requires<ArgumentNullException>(paramIndex != null);

			string p = paramName.ToString();
			int j = mParams.IndexOf(p);

			if (j < 0)
				throw new PqsqlException("Could not find parameter «" + p + "» in PqsqlCommand.Parameters");

			paramIndex.Append(j + 1);
			statement.Append(paramIndex);

			paramIndex.Length = 1;
			paramName.Length = 1;
		}

		/// <summary>
		/// split PqsqlCommand.CommandText into an array of sub-statements
		/// </summary>
		/// <returns></returns>
		private string[] ParseStatements()
		{
			Contract.Ensures(Contract.Result<string[]>() != null);

			string[] statements = new string[0];

			int parsingState = 0; // 1...quote, 2...dollarQuote, ...

			StringBuilder stmt = new StringBuilder(); // buffer for current statement
			int stmtNum = 0; // index in statements
			
			StringBuilder paramName = new StringBuilder();  // :param
			paramName.Append(':');
			StringBuilder paramIndex = new StringBuilder(); // $i
			paramIndex.Append('$');

			Contract.Assume(CommandText != null);

			//
			// parse multiple statements separated by ';'
			// - ignore ', ", and $$ quotation
			// - escape character \ during quotation
			// - replace :[a-zA-Z0-9_]+ parameter names with $ index
			//
			foreach (char c in CommandText.Trim())
			{
				if ((parsingState & (QUOTE | ESCAPE)) == (QUOTE | ESCAPE)) // eat next character, continue without ESCAPE
				{
					parsingState &= ~ESCAPE;
				}
				else if ((parsingState & QUOTE) == QUOTE) // eat input until next quote symbol
				{
					switch (c)
					{
						case '\\':
							parsingState |= ESCAPE;
							break;

						case '\'':
						case '"':
							stmt.Append(c);
							parsingState &= ~QUOTE;
							continue;
					}
				}
				else if ((parsingState & PARAM0) == PARAM0) // did we really ran into :[a-zA-Z0-9_]+ ?
				{
					if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_') // :[a-zA-Z0-9_]+
					{
						paramName.Append(c); // start eating first character of form [a-zA-Z0-9_]
						parsingState |= PARAM1;
						parsingState &= ~PARAM0;
					}
					else // we probably ran into :: or :=
					{
						Contract.Assume(stmt != null);
						stmt.Append(':'); // take first : and put it back
						stmt.Append(c); // take current character and put it back
						paramName.Length = 1;
						parsingState &= ~(PARAM0 | PARAM1);
					}
					continue;
				}
				else if ((parsingState & PARAM1) == PARAM1) // save parameter name to paramName; replace with $ index
				{
					if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_') // :[a-zA-Z0-9_]+
					{
						paramName.Append(c); // eat parameter name of form [a-zA-Z0-9_]
						continue;
					}

					// replace parameter name with $ index
					Contract.Assume(stmt != null);
					ReplaceParameter(ref stmt, ref paramName, ref paramIndex);

					// now continue with parsing statement(s)
					parsingState &= ~(PARAM0 | PARAM1);
				}
				else if ((parsingState & (DOLLARQUOTE0 | DOLLARQUOTE1)) == (DOLLARQUOTE0 | DOLLARQUOTE1)) // found $$ previously, ignore everything except $
				{
					if (c == '$') parsingState &= ~DOLLARQUOTE1;
				}
				else if ((parsingState & DOLLARQUOTE0) == DOLLARQUOTE0) // found $ before
				{
					if (char.IsDigit(c)) // we might parse $[0-9]
					{
						if ((parsingState & DOLLARQUOTE) == DOLLARQUOTE)
							parsingState |= DOLLARQUOTE1; // back to $$ mode
						else
							parsingState &= ~DOLLARQUOTE0; // found $[0-9] outside of $$, back to standard mode
					}
					else if (c == '$') // no digit after $, check for closing/beginning $
					{
						if ((parsingState & DOLLARQUOTE) == DOLLARQUOTE)
							parsingState &= ~(DOLLARQUOTE | DOLLARQUOTE0); // closing $$
						else
							parsingState |= (DOLLARQUOTE | DOLLARQUOTE0); // beginning $$
					}
				}

				// before we save, check whether we need to update the parsingState
				if (parsingState == 0) 
				{
					switch (c) // eat input until ; with quotation and parameter dispatching
					{
						case '$':
							parsingState |= (DOLLARQUOTE | DOLLARQUOTE0);
							break;

						case '\'':
						case '"':
							parsingState |= QUOTE;
							break;

						case ':':
							parsingState |= PARAM0;
						  continue;

						case ';':
							Contract.Assume(stmt != null);
							ResizeAndSetStatements(ref stmt, ref statements, stmtNum++);
							continue;
					}
				}

				// save character into next statement
				stmt.Append(c);
			}

			Contract.Assume(stmt != null);
			if (stmt.Length > 0) // add last statement not terminated by ';'
			{
				if (paramName.Length > 1) // did not finish replacing parameter name
					ReplaceParameter(ref stmt, ref paramName, ref paramIndex);

				ResizeAndSetStatements(ref stmt, ref statements, stmtNum);
			}

			return statements;
		}

		#endregion

		//
		// Summary:
		//     Creates a prepared (or compiled) version of the command on the data source.
		public override void Prepare()
		{
			throw new NotImplementedException("Prepare() is not implemented");
		}
	}
}
