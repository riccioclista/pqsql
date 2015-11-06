using System;
using System.Text;
using System.Data.Common;
using System.ComponentModel;
using System.Data;

namespace Pqsql
{
	public class PqsqlCommand : DbCommand
	{
		const string mStatementTimeoutString = "set statement_timeout=";
		const string mStoredProcString = "select * from ";
		const string mTableString = "table ";

		protected string mCmdText;

		protected int mCmdTimeout;
		protected bool mCmdTimeoutSet;

		protected CommandType mCmdType;

		protected PqsqlConnection mConn;

		protected readonly PqsqlParameterCollection mParams;

		protected PqsqlTransaction mTransaction;

		protected UpdateRowSource mUpdateRowSource = UpdateRowSource.Both;


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

		protected void Init(string q)
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

						err = PqsqlProviderFactory.Instance.CreateStringFromUTF8(new IntPtr(b));
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
				r.Read(); // read first row
				int i = 0;
				foreach (PqsqlParameter p in Parameters)
				{
					if (p.Direction != ParameterDirection.Input)
					{
						p.Value = r.GetValue(i++);
					}
				}
				r.Consume(); // sync protocol: consume remaining rows
			}

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
			string[] statements = new string[0];

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
				if (p.Direction != ParameterDirection.Output)
				{
					if (comma)
					{
						spq.Append(',');
					}
					spq.Append('$');
					spq.Append(i);
					comma = true;
				}
				i++;
			}

			spq.Append(')');

			Array.Resize(ref statements, 1);
			statements[0] = spq.ToString();
		}

		// open connection if it is closed or broken
		protected void CheckOpen()
		{
			ConnectionState s = mConn.State;

			if (s == ConnectionState.Closed || (s & ConnectionState.Broken) > 0)
			{
				mConn.Open();
			}
		}


		// sets statement_timeout of the current session
		protected void SetStatementTimeout()
		{
			if (mCmdTimeout > 0 && mCmdTimeoutSet == false)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(mStatementTimeoutString);
				sb.Append(CommandTimeout);
				byte[] stmtTimeout = PqsqlProviderFactory.Instance.CreateUTF8Statement(sb);
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
			return r.GetValue(0);
		}


		// replace parameter names :ParameterName with parameter index $j
		protected void ReplaceParameterNames(ref StringBuilder sb)
		{
			if (mParams == null)
				return;

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
		protected void ParseStatements(ref string[] statements)
		{
			StringBuilder sb = new StringBuilder();
			bool quote = false;
			bool dollarQuote = false;
			bool dollarQuote0 = false;
			bool dollarQuote1 = false;
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
							quote = false;
							break;
					}
					sb.Append(c);
				}
				else if (dollarQuote0 && dollarQuote1) // found $$ previously, ignore everything except $
				{
					switch (c)
					{
						case '$':
							dollarQuote1 = false;
							break;
					}
					sb.Append(c);
				}
				else if (dollarQuote0) // found $ before
				{
					if (char.IsDigit(c))
					{
						if (dollarQuote) dollarQuote1 = true; // back to $$ mode
						else dollarQuote0 = false; // back to standard mode
					}
					else
					{
						switch (c)
						{
						case '$':
							if (dollarQuote)
							{
								dollarQuote = false; // closing $$
								dollarQuote0 = false;
							}
							else
							{
								dollarQuote = true; // beginning $$
								dollarQuote1 = true;
							}
							break;
						}
					}
					sb.Append(c);
				}
				else
				{
					switch (c) // eat input until ;
					{
						case '$':
							dollarQuote0 = true;
							break;

						case ';':
							Array.Resize(ref statements, stmLen + 1);
							ReplaceParameterNames(ref sb);
							statements[stmLen++] = sb.ToString();
							sb.Clear();
							continue;

						case '\'':
						case '"':
							quote = true;
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
