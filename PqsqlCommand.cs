using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

using PqsqlWrapper = Pqsql.UnsafeNativeMethods.PqsqlWrapper;
using PqsqlBinaryFormat = Pqsql.UnsafeNativeMethods.PqsqlBinaryFormat;

namespace Pqsql
{
	public sealed class PqsqlCommand : DbCommand
	{
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
					statements = ParseStatements().ToArray();
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

			// always try to set statement_timeout, the session started 
			// with the PqsqlDataReader below will then have this timeout
			// until we issue the next CommandTimeout
			if (mCmdTimeout > 0)
			{
				mConn.SetSessionParameter(PqsqlClientConfiguration.StatementTimeout, CommandTimeout);
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

			StringBuilder tq = new StringBuilder(mTableString);
			tq.Append(CommandText);

			return new string[] { tq.ToString() };
		}

		private string[] BuildStoredProcStatement()
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<string[]>() != null);
#endif

			StringBuilder spq = new StringBuilder(mStoredProcString);
			spq.Append(CommandText);
			spq.Append('(');

			// add parameter index $i for each IN and INOUT parameter
			int i = 1;
			bool comma = false;

			// in and inout parameter are positional in PqsqlParameterBuffer
			// out, inout, and return values are set using their column names in PqsqlDataReader

			foreach (PqsqlParameter p in Parameters)
			{
				ParameterDirection direction = p.Direction;

				if (direction == ParameterDirection.Output || direction == ParameterDirection.ReturnValue)
					continue;

				if (comma)
				{
					spq.Append(',');
				}
				spq.Append('$');
				spq.Append(i++);
				comma = true;
			}

			// we create the query string
			//    select * from Func($1,...) as "Func";
			// when CommandText == "Func"
			//
			// + if func(.) defines out/inout variables (prorettype is record),
			//   then the result columns will just be named after the output variables.
			// + otherwise, if func(.) has prorettype != record
			//   (e.g., create function (i int) returns int as 'begin return i; end' ...),  then we
			//   get a predictable output column name called "Func" in the result record. The
			//   problem is that whenever we prefix the function name with a schema name or use
			//   mixed-case CommandText strings (e.g., "public"."func" or FuNc), the result column
			//   would just be called "func" without "as \"Func\"".

			spq.Append(") as \"");
			spq.Append(CommandText.Trim().Replace("\"", "\"\"")); // escape double quotes
			spq.Append("\";");

			return new string[] { spq.ToString() };
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

		/// <summary>
		/// split PqsqlCommand.CommandText into an array of sub-statements
		/// </summary>
		/// <returns></returns>
		private IEnumerable<string> ParseStatements()
		{
#if CODECONTRACTS
			Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);
#endif

			IntPtr pstate = IntPtr.Zero;
			IntPtr varArray = IntPtr.Zero;	

			try
			{
				unsafe
				{
					int offset = 0;
					varArray = Marshal.AllocHGlobal((mParams.Count + 1) * sizeof(sbyte*));
					
					// always write NULL before we continue, so finally clause can clean up properly
					// if we get hit by an exception
					Marshal.WriteIntPtr(varArray, offset, IntPtr.Zero);

					foreach (PqsqlParameter param in mParams)
					{
						// always write NULL before we continue, so finally clause can clean up properly
						// if we get hit by an exception
						Marshal.WriteIntPtr(varArray, offset, IntPtr.Zero);

						string psqlParamName = param.PsqlParameterName;

						// psql-specific: characters allowed in variable names: [A-Za-z\200-\377_0-9]
						// we only allow lowercase [a-z0-9_], as PsqlParameter always stores parameter names in lowercase
						char invalid = psqlParamName.FirstOrDefault(c => !(c >= 'a' && c <= 'z') && !char.IsDigit(c) && c != '_');
						if (invalid != default(char))
						{
							string msg = string.Format(CultureInfo.InvariantCulture, "Parameter name «{0}» contains invalid character «{1}»", psqlParamName, invalid);
							throw new PqsqlException(msg, (int) PqsqlState.SYNTAX_ERROR);
						}

						// variable names are pure ascii
						byte[] paramNameArray = Encoding.ASCII.GetBytes(psqlParamName);
						int len = paramNameArray.Length;

						// we need a null-terminated variable string
						IntPtr varString = Marshal.AllocHGlobal(len + 1);
						Marshal.Copy(paramNameArray, 0, varString, len);
						Marshal.WriteByte(varString, len, 0);

						Marshal.WriteIntPtr(varArray, offset, varString);
						offset += sizeof(sbyte*);
					}

					Marshal.WriteIntPtr(varArray, offset, IntPtr.Zero);
				}
				
				// varArray pointers must be valid during parsing
				pstate = PqsqlBinaryFormat.pqparse_init(varArray);

				// always terminate CommandText with a ; (prevents unnecessary re-parsing)
				// we have the following cases:
				// 1) "select 1; select 2"
				// 2) "select 1; select 2 -- dash-dash comment forces newline for semicolon"
				// 3) "select 1; select 2; /* slash-star comment forces unnecessary semicolon */"
				// 4) "select 1; select 2 -- dash-dash comment triggers ;"
				//
				// For (1), (2), (3) we simply add a newline + semicolon.  Case (4) is more tricky
				// and requires to re-start the parser for another round.
				string commands = CommandText.TrimEnd();
				if (!commands.EndsWith(";", StringComparison.Ordinal))
				{
					commands += "\n;";
				}

				byte[] statementsString = PqsqlUTF8Statement.CreateUTF8Statement(commands);

				// add a semicolon-separated list of UTF-8 statements
				int parsingState = PqsqlBinaryFormat.pqparse_add_statements(pstate, statementsString);

				if (parsingState == -1) // syntax error or missing parameter
				{
					ParsingError(pstate);
				}
				else if (parsingState == 1) // incomplete input, continue with current parsing state and force final "\n;"
				{
					statementsString = PqsqlUTF8Statement.CreateUTF8Statement("\n;");
					if (PqsqlBinaryFormat.pqparse_add_statements(pstate, statementsString) != 0)
					{
						ParsingError(pstate); // syntax error / missing parameter / incomplete input
					}
				}

				uint num = PqsqlBinaryFormat.pqparse_num_statements(pstate);

				string[] statements = new string[num];

				// the null-terminated array of UTF-8 statement strings
				IntPtr sptr = PqsqlBinaryFormat.pqparse_get_statements(pstate);

				if (num > 0 && sptr != IntPtr.Zero)
				{
					unsafe
					{
						for (int i = 0; i < num; i++)
						{
							sbyte** stm = (sbyte**) sptr.ToPointer();

							if (stm == null || *stm == null)
								break;

							// convert UTF-8 to UTF-16
							statements[i] = PqsqlUTF8Statement.CreateStringFromUTF8(new IntPtr(*stm));
							sptr = IntPtr.Add(sptr, sizeof(sbyte*));
						}					
					}
				}

#if CODECONTRACTS
				Contract.Assert(statements != null);
				Contract.Assert(statements.Length == num);
#endif

				return from statement in statements
					   where !string.IsNullOrWhiteSpace(statement) && statement != ";"
					   select statement;
			}
			finally
			{
				if (pstate != IntPtr.Zero)
				{
					PqsqlBinaryFormat.pqparse_destroy(pstate);
				}

				if (varArray != IntPtr.Zero)
				{
					unsafe
					{
						for (int i = mParams.Count - 1; i >= 0; i--)
						{
							IntPtr varPtr = Marshal.ReadIntPtr(varArray, i * sizeof(sbyte*));

							if (varPtr != IntPtr.Zero)
							{
								Marshal.FreeHGlobal(varPtr);
							}
						}
					}
					Marshal.FreeHGlobal(varArray);
				}
			}
		}

		private void ParsingError(IntPtr pstate)
		{
			string msg;
			int unknown = PqsqlBinaryFormat.pqparse_num_unknown_variables(pstate);

			if (unknown != 0)
			{
				int numParams = 0;
				StringBuilder paramList = new StringBuilder();

				foreach (PqsqlParameter param in mParams)
				{
					if (numParams > 0)
						paramList.Append(',');
					numParams++;
					paramList.Append(param.PsqlParameterName);
					if (numParams > 128)
					{
						paramList.Append(",...");
						break;
					}
				}

				msg = string.Format(CultureInfo.InvariantCulture,
					"Could not substitute {0} variable name(s) in «{1}» using PqsqlCommand.Parameters «{2}»", unknown, CommandText,
					paramList);
			}
			else
			{
				msg = string.Format(CultureInfo.InvariantCulture, "Syntax error in «{0}»", CommandText);
			}

			throw new PqsqlException(msg, (int) PqsqlState.SYNTAX_ERROR);
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
