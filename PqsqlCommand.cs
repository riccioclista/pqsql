using System;
using System.Text;
using System.Data.Common;
using System.ComponentModel;
using System.Data;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

using PqsqlWrapper = Pqsql.UnsafeNativeMethods.PqsqlWrapper;

namespace Pqsql
{
	public sealed class PqsqlCommand : DbCommand
	{
		private const string mStatementTimeoutString = "statement_timeout";

		private const string mStoredProcString = "select * from ";

		private const string mTableString = "table ";

		private string mCmdText;

		// negative values or 0 => session will be kept as is
		private int mCmdTimeout;

		private CommandType mCmdType;

		private CommandBehavior mCmdBehavior;

		private PqsqlConnection mConn;

		private readonly PqsqlParameterCollection mParams;

		private PqsqlTransaction mTransaction;

		private UpdateRowSource mUpdateRowSource = UpdateRowSource.Both;

#if CODECONTRACTS
		[ContractInvariantMethod]
		private void ClassInvariant()
		{
			Contract.Invariant(mParams != null);
		}
#endif

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
			mCmdType = CommandType.Text;
			mCmdBehavior = CommandBehavior.Default;
		}

		#region Overrides of Component

		private bool mDisposed;

		protected override void Dispose(bool disposing)
		{
			if (mDisposed)
			{
				return;
			}

			if (disposing)
			{
				// give up references to transaction and connection
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
					throw new ArgumentNullException(nameof(value));
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
				mCmdTimeout = (int) Math.Min((long) value * 1000, int.MaxValue); // mCmdTimeout is in msecs
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
#if CODECONTRACTS
				Contract.Ensures(Contract.Result<PqsqlParameterCollection>() != null);
#endif

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
						throw new ArgumentOutOfRangeException(nameof(value));
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

						err = PqsqlUTF8Statement.CreateStringFromUTF8((byte*)b);
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
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<PqsqlParameter>() != null);
#endif

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

			int ra = Math.Max(-1, r.RecordsAffected);
			r.Consume(); // sync protocol: consume remaining rows

			if ((mCmdBehavior & CommandBehavior.SingleResult) == CommandBehavior.SingleResult) // only one statement available
			{
				return ra;
			}

			// we have more than one statement
			while (r.NextResult())
			{
				int n = r.RecordsAffected;

				// accumulate positive RecordsAffected for each UPDATE / DELETE / INSERT / CREATE * / ... statement
				if (n >= 0)
				{
					if (ra < 0)
					{
						ra = n;
					}
					else // ra >= 0
					{
						ra += n;
					}
				}

				r.Consume(); // sync protocol: consume remaining rows
			}

			int last = r.RecordsAffected;

			// accumulate positive RecordsAffected for each UPDATE / DELETE / INSERT / CREATE * / ... statement
			if (last >= 0)
			{
				if (ra < 0)
				{
					ra = last;
				}
				else // ra >= 0
				{
					ra += last;
				}
			}

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
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<PqsqlDataReader>() != null);
#endif

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
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<PqsqlDataReader>() != null);
#endif

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

#if CODECONTRACTS
			Contract.Assert(statements != null);
#endif

			if (statements.Length < 2)
				behavior |= CommandBehavior.SingleResult;

			CheckOpen();

#if CODECONTRACTS
			Contract.Assert(mConn != null);
#endif

			// always set application_name, after a DISCARD ALL (usually issued by pgbouncer)
			// the session information is gone forever, and the shared connection will drop
			// application_name
			string appname = mConn.ApplicationName;
			if (!string.IsNullOrEmpty(appname))
			{
				SetSessionParameter(PqsqlConnectionStringBuilder.application_name, appname, true);
			}

			// always try to set statement_timeout, the session started 
			// with the PqsqlDataReader below will then have this timeout
			// until we return
			if (mCmdTimeout > 0)
			{
				SetSessionParameter(mStatementTimeoutString, CommandTimeout, false);
			}

			// save behavior
			mCmdBehavior = behavior;

			PqsqlDataReader r = null;
			PqsqlDataReader reader;

			try
			{
				r = new PqsqlDataReader(this, behavior, statements);
				r.NextResult(); // always execute first command

				// swap r with reader
				reader = r;
				r = null;
			}
			finally
			{
				if (r != null) // only dispose PqsqlDataReader if r.NextResult() throwed an exception
				{
					r.Close();
					r.Dispose();
				}
			}

			return reader;
		}

		private string[] BuildTableStatement()
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<string[]>() != null);
#endif

			string[] statements = new string[1];

			StringBuilder tq = new StringBuilder(mTableString);
			tq.Append(CommandText);

			statements[0] = tq.ToString();

			return statements;
		}

		private string[] BuildStoredProcStatement()
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<string[]>() != null);
#endif

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
#if CODECONTRACTS
			Contract.Assume(mConn != null);
#endif

			ConnectionState s = mConn.State;

			if (s == ConnectionState.Closed || (s & ConnectionState.Broken) > 0)
			{
				mConn.Open();
			}
		}

		// executes SET parameter=value
		private void SetSessionParameter(string parameter, object value, bool quote)
		{
#if CODECONTRACTS
			Contract.Assume(mConn != null);
#endif

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
		private static bool ResizeAndSetStatements(ref StringBuilder statement, ref string[] statements, int i)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(statements != null);
			Contract.Requires<ArgumentNullException>(statement != null);
			Contract.Requires<ArgumentNullException>(i >= 0);
			Contract.Ensures(statement != null);
			Contract.Ensures(statements != null);
#else
			if (statements == null)
				throw new ArgumentNullException(nameof(statements));
			if (statement == null)
				throw new ArgumentNullException(nameof(statement));

			if (i < 0)
				throw new ArgumentOutOfRangeException(nameof(i));
#endif

			string stm = statement.ToString().TrimStart();
			bool isnonempty = !string.IsNullOrWhiteSpace(stm);

			// ignore empty statements
			if (isnonempty)
			{
				Array.Resize(ref statements, i + 1);
				statements[i] = stm;
			}
			statement.Clear();

			return isnonempty;
		}

		// replace parameter name with $ index in statement
		private void ReplaceParameter(ref StringBuilder statement, ref StringBuilder paramName, ref StringBuilder paramIndex)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(statement != null);
			Contract.Requires<ArgumentNullException>(paramName != null);
			Contract.Requires<ArgumentNullException>(paramIndex != null);
			Contract.Ensures(statement != null);
			Contract.Ensures(paramName != null);
			Contract.Ensures(paramIndex != null);
#else
			if (statement == null)
				throw new ArgumentNullException(nameof(statement));
			if (paramName == null)
				throw new ArgumentNullException(nameof(paramName));
			if (paramIndex == null)
				throw new ArgumentNullException(nameof(paramIndex));
#endif

			string p = paramName.ToString();
			int j = mParams.IndexOf(p);

			if (j < 0)
				throw new PqsqlException("Could not find parameter «" + p + "» in PqsqlCommand.Parameters", (int) PqsqlState.UNDEFINED_PARAMETER);

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
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<string[]>() != null);
#endif

			string[] statements = new string[0];

			int parsingState = 0; // 1...quote, 2...dollarQuote, ...

			StringBuilder stmt = new StringBuilder(); // buffer for current statement
			int stmtNum = 0; // index in statements
			
			StringBuilder paramName = new StringBuilder();  // :param
			paramName.Append(':');
			StringBuilder paramIndex = new StringBuilder(); // $i
			paramIndex.Append('$');

#if CODECONTRACTS
			Contract.Assume(CommandText != null);
#endif

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
#if CODECONTRACTS
						Contract.Assert(stmt != null);
#endif
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
#if CODECONTRACTS
					Contract.Assert(stmt != null);
					Contract.Assert(paramIndex != null);
					Contract.Assert(paramName != null);
#endif
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
#if CODECONTRACTS
							Contract.Assert(stmt != null);
#endif
							if (ResizeAndSetStatements(ref stmt, ref statements, stmtNum))
							{
								stmtNum++;
							}
							continue;
					}
				}

				// save character into next statement
				stmt.Append(c);
			}

#if CODECONTRACTS
			Contract.Assert(stmt != null);
			Contract.Assert(statements != null);
#endif
			if (stmt.Length > 0) // add last statement not terminated by ';'
			{
				if (paramName.Length > 1) // did not finish replacing parameter name
					ReplaceParameter(ref stmt, ref paramName, ref paramIndex);

				ResizeAndSetStatements(ref stmt, ref statements, stmtNum);
			}

#if CODECONTRACTS
			Contract.Assert(Contract.ForAll(statements, s => !string.IsNullOrWhiteSpace(s)));
#endif

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
