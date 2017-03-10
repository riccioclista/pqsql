using System;
using System.Data.Common;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif

namespace Pqsql
{
	public sealed class PqsqlDataAdapter : DbDataAdapter
	{

		public PqsqlDataAdapter()
		{
		}

		public PqsqlDataAdapter(DbCommand selectCommand)
		{
			SelectCommand = selectCommand;
		}

		public PqsqlDataAdapter(string selectCommandText, PqsqlConnection selectConnection)
		{
			PqsqlCommand cmd = null;

			try
			{
				cmd = new PqsqlCommand(selectCommandText, selectConnection);
				SelectCommand = cmd;

				cmd = null;
			}
			finally
			{
				cmd?.Dispose();
			}
		}

		public PqsqlDataAdapter(string selectCommandText, string selectConnectionString)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(selectCommandText != null);
			Contract.Requires<ArgumentNullException>(selectConnectionString != null);
#else
			if (selectCommandText == null)
				throw new ArgumentNullException(nameof(selectCommandText));
			if (selectConnectionString == null)
				throw new ArgumentNullException(nameof(selectConnectionString));
#endif

			PqsqlConnection conn = null;
			PqsqlCommand cmd = null;

			try
			{
				conn = new PqsqlConnection(selectConnectionString);
				
				// ReSharper disable once UseObjectOrCollectionInitializer
				cmd = new PqsqlCommand();
				cmd.CommandText = selectCommandText;
				cmd.Connection = conn;

				SelectCommand = cmd;

				cmd = null;
				conn = null;
			}
			finally
			{
				cmd?.Dispose();
				conn?.Dispose();
			}
		}

		/// <summary>
		/// Row updating event.
		/// </summary>
		public event EventHandler<RowUpdatingEventArgs> RowUpdating;

		/// <summary>
		/// Row updated event.
		/// </summary>
		public event EventHandler<RowUpdatedEventArgs> RowUpdated;

		/// <summary>
		/// Raises the RowUpdating event of a .NET Framework data provider.
		/// </summary>
		/// <param name="value">An <see cref="T:System.Data.Common.RowUpdatingEventArgs"/>  that contains the event data. </param>
		protected override void OnRowUpdating(RowUpdatingEventArgs value)
		{
			RowUpdating?.Invoke(this, value);
		}

		/// <summary>
		/// Raises the RowUpdated event of a .NET Framework data provider.
		/// </summary>
		/// <param name="value">A <see cref="T:System.Data.Common.RowUpdatedEventArgs"/> that contains the event data. </param>
		protected override void OnRowUpdated(RowUpdatedEventArgs value)
		{
			RowUpdated?.Invoke(this, value);
		}

	}
}
