using System;
using System.Data.Common;

namespace Pqsql
{

	/// <summary>
	/// Represents the method that handles the <see cref="PqsqlDataAdapter.RowUpdating">RowUpdating</see> events.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">A <see cref="RowUpdatingEventArgs">RowUpdatingEventArgs</see> that contains the event data.</param>
	public delegate void PqsqlRowUpdatingEventHandler(object sender, RowUpdatingEventArgs e);

	/// <summary>
	/// Represents the method that handles the <see cref="PqsqlDataAdapter.RowUpdated">RowUpdated</see> events.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">A <see cref="RowUpdatedEventArgs">RowUpdatedEventArgs</see> that contains the event data.</param>
	public delegate void PqsqlRowUpdatedEventHandler(object sender, RowUpdatedEventArgs e);


	public sealed class PqsqlDataAdapter : DbDataAdapter
	{

		public PqsqlDataAdapter()
		{
		}

		public PqsqlDataAdapter(DbCommand selectCommand)
    {
      SelectCommand = selectCommand;
    }

		public PqsqlDataAdapter(String selectCommandText, PqsqlConnection selectConnection)
			: this(new PqsqlCommand(selectCommandText, selectConnection))
    {
		}

		public PqsqlDataAdapter(String selectCommandText, String selectConnectionString)
			: this(selectCommandText, new PqsqlConnection(selectConnectionString))
    {
		}

		/// <summary>
		/// Row updating event.
		/// </summary>
		public event PqsqlRowUpdatingEventHandler RowUpdating;

		/// <summary>
		/// Row updated event.
		/// </summary>
		public event PqsqlRowUpdatedEventHandler RowUpdated;

		/// <summary>
		/// Raises the RowUpdating event of a .NET Framework data provider.
		/// </summary>
		/// <param name="value">An <see cref="T:System.Data.Common.RowUpdatingEventArgs"/>  that contains the event data. </param>
		protected override void OnRowUpdating(RowUpdatingEventArgs value)
		{
			PqsqlRowUpdatingEventHandler handler = RowUpdating;
			if (handler != null)
			{
				handler(this, value);
			}
		}

		/// <summary>
		/// Raises the RowUpdated event of a .NET Framework data provider.
		/// </summary>
		/// <param name="value">A <see cref="T:System.Data.Common.RowUpdatedEventArgs"/> that contains the event data. </param>
		protected override void OnRowUpdated(RowUpdatedEventArgs value)
		{
			PqsqlRowUpdatedEventHandler handler = RowUpdated;
			if (handler != null)
			{
				handler(this, value);
			}
		}

	}
}
