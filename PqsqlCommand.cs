using System;
using System.Text;
using System.Data.Common;
using System.ComponentModel;
using System.Data;

namespace Pqsql
{
	public sealed class PqsqlCommand : DbCommand
	{
		const string mStatementTimeoutString = "set statement_timeout=";
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
				
				throw new PqsqlException("Could not cancel command: " + err);
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
			string[] statements = new string[0];

			// always set SingleRow mode for now,
			// will be turned off for FETCH statements in PqsqlDataReader.Execute()
			behavior |= CommandBehavior.SingleRow;

			switch(CommandType)
			{
				case CommandType.Text:
					ParseStatements(ref statements);
					break;

				case CommandType.StoredProcedure:
					BuildStoredProcStatement(ref statements);
					break;

				case CommandType.TableDirect:
					BuildTableStatement(ref statements);
					break;
			}

			if (statements.Length < 2)
				behavior |= CommandBehavior.SingleResult;

			CheckOpen();

			SetStatementTimeout();

			PqsqlDataReader reader = new PqsqlDataReader(this, behavior, statements);
			reader.NextResult(); // always execute first command
			return reader;
		}

		private void BuildTableStatement(ref string[] statements)
		{
			StringBuilder tq = new StringBuilder(mTableString);
			tq.Append(CommandText);
			Array.Resize(ref statements, 1);
			statements[0] = tq.ToString();
		}

		private void BuildStoredProcStatement(ref string[] statements)
		{
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

			Array.Resize(ref statements, 1);
			statements[0] = spq.ToString();
		}

		// open connection if it is closed or broken
		private void CheckOpen()
		{
			ConnectionState s = mConn.State;

			if (s == ConnectionState.Closed || (s & ConnectionState.Broken) > 0)
			{
				mConn.Open();
			}
		}


		// sets statement_timeout of the current session
		private void SetStatementTimeout()
		{
			if (mCmdTimeout > 0 && mCmdTimeoutSet == false)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(mStatementTimeoutString);
				sb.Append(CommandTimeout);
				byte[] stmtTimeout = PqsqlUTF8Statement.CreateUTF8Statement(sb);
				IntPtr res;
				IntPtr pc = mConn.PGConnection;

				unsafe
				{
					fixed (byte* st = stmtTimeout)
					{
						res = PqsqlWrapper.PQexec(pc, st);
					}
				}

				if (res != IntPtr.Zero)
				{
					ExecStatus s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);

					PqsqlWrapper.PQclear(res);

					if (s == ExecStatus.PGRES_COMMAND_OK)
					{
						mCmdTimeoutSet = true;
						return;
					}
				}

				string err = mConn.GetErrorMessage();
				throw new PqsqlException(err);
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

		// statement parser states
		const int QUOTE = (1 << 0);
		const int DOLLARQUOTE = (1 << 1);
		const int DOLLARQUOTE0 = (1 << 2);
		const int DOLLARQUOTE1 = (1 << 3);
		const int PARAM0 = (1 << 4);
		const int PARAM1 = (1 << 5);
		const int ESCAPE = (1 << 6);

		delegate void ResizeAndSetStatements(ref StringBuilder sb, ref string[] st, int i);

		/// <summary>
		/// split PqsqlCommand.CommandText into an array of sub-statements
		/// </summary>
		/// <returns></returns>
		private void ParseStatements(ref string[] statements)
		{
			int parsingState = 0; // 1...quote, 2...dollarQuote, ...

			StringBuilder stmt = new StringBuilder(); // buffer for current statement
			int stmtNum = 0; // index in statements
			
			StringBuilder paramName = new StringBuilder();  // :param
			paramName.Append(':');
			StringBuilder paramIndex = new StringBuilder(); // $i
			paramIndex.Append('$');


			ResizeAndSetStatements resizeAndSet = (ref StringBuilder sb, ref string[] st, int i) => {
				Array.Resize(ref st, i + 1);
				st[i] = sb.ToString().TrimStart();
				sb.Clear();
			};

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
				else if ((parsingState & PARAM0) == PARAM0) // did we really ran into :param ?
				{
					if (c == ':') // we ran into ::
					{
						stmt.Append(':'); // take first : and put it back
						stmt.Append(':'); // take current : and put it back
						paramName.Length = 1;
						parsingState &= ~(PARAM0 | PARAM1);
					}
					else
					{
						paramName.Append(c); // start eating first character of parameter names
						parsingState |= PARAM1;
						parsingState &= ~PARAM0;
					}
					continue;
				}
				else if ((parsingState & PARAM1) == PARAM1) // save parameter name to paramName; replace with $ index
				{
					if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
					{
						paramName.Append(c); // eat parameter name of form [a-zA-Z0-9_]
						continue;
					}

					// replace parameter name with $ index
					string pname = paramName.ToString();
					int j = mParams.IndexOf(pname);

					if (j < 0)
						throw new PqsqlException("Could not find parameter »" + pname + "« in PqsqlCommand.Parameters");

					paramIndex.Append(j + 1);
					stmt.Append(paramIndex);
						
					paramIndex.Length = 1;
					paramName.Length = 1;

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
							resizeAndSet(ref stmt, ref statements, stmtNum++);
							continue;
					}
				}

				// save character into next statement
				stmt.Append(c);
			}

			if (stmt.Length > 0) // add last statement not terminated by ';'
			{
				resizeAndSet(ref stmt, ref statements, stmtNum);
			}
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
