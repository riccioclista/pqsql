using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Pqsql
{
	// enum PostgresPollingStatusType from libpq-fe.h
	internal enum PostgresPollingStatus
	{
		PGRES_POLLING_FAILED = 0,
		PGRES_POLLING_READING,      /* These two indicate that one may    */
		PGRES_POLLING_WRITING,      /* use select before polling again.   */
		PGRES_POLLING_OK,
		PGRES_POLLING_ACTIVE        /* unused; keep for awhile for backwards compatibility */
	};

	// enum ConnectionStatusType from libpq-fe.h
	internal enum ConnectionStatus
	{
		CONNECTION_OK = 0,
		CONNECTION_BAD,
		/* Non-blocking mode only below here */

		/*
		 * The existence of these should never be relied upon - they should only
		 * be used for user feedback or similar purposes.
		 */
		CONNECTION_STARTED,			/* Waiting for connection to be made.  */
		CONNECTION_MADE,			  /* Connection OK; waiting to send.     */
		CONNECTION_AWAITING_RESPONSE,		/* Waiting for a response from the postmaster. */
		CONNECTION_AUTH_OK,			/* Received authentication; waiting for backend startup. */
		CONNECTION_SETENV,			/* Negotiating environment. */
		CONNECTION_SSL_STARTUP,	/* Negotiating SSL. */
		CONNECTION_NEEDED			  /* Internal state: connect() needed */
	};

	// enum ExecStatusType from libpq-fe.h
	internal enum ExecStatus
	{
		PGRES_EMPTY_QUERY = 0, /* empty query string was executed */
		PGRES_COMMAND_OK,			 /* a query command that doesn't return anything was executed properly by the backend */
		PGRES_TUPLES_OK,			 /* a query command that returns tuples was executed properly by the backend, PGresult contains the result tuples */
		PGRES_COPY_OUT,				 /* Copy Out data transfer in progress */
		PGRES_COPY_IN,				 /* Copy In data transfer in progress */
		PGRES_BAD_RESPONSE,		 /* an unexpected response was recv'd from the backend */
		PGRES_NONFATAL_ERROR,	 /* notice or warning message */
		PGRES_FATAL_ERROR,		 /* query failed */
		PGRES_COPY_BOTH,			 /* Copy In/Out data transfer in progress */
		PGRES_SINGLE_TUPLE		 /* single tuple from larger resultset */
	};

	// see enum PGTransactionStatus in libpq-fe.h
	internal enum PGTransactionStatus
	{
		PQTRANS_IDLE,               /* connection idle */
		PQTRANS_ACTIVE,             /* command in progress */
		PQTRANS_INTRANS,            /* idle, within transaction block */
		PQTRANS_INERROR,            /* idle, within failed transaction */
		PQTRANS_UNKNOWN             /* cannot determine status */
	};


	/// <summary>
	/// wraps C functions from libpq.dll
	/// </summary>
	/// <remarks>https://msdn.microsoft.com/en-us/library/system.security.suppressunmanagedcodesecurityattribute%28v=vs.100%29.aspx</remarks>
	[SuppressUnmanagedCodeSecurity]
	internal class PqsqlWrapper
	{
		// libpq.dll depends on libeay32.dll, libintl-8.dll, ssleay32.dll
		// (DllImport would throw a DllNotFoundException if some of them are missing)


		// Note: On Windows, there is a way to improve performance if a single database connection is repeatedly started and shutdown. Internally, libpq calls WSAStartup() and WSACleanup() for connection startup and shutdown, respectively. WSAStartup() increments an internal Windows library reference count which is decremented by WSACleanup(). When the reference count is just one, calling WSACleanup() frees all resources and all DLLs are unloaded. This is an expensive operation. To avoid this, an application can manually call WSAStartup() so resources will not be freed when the last database connection is closed.

		#region libpq setup

		[DllImport("libpq.dll")]
		public static extern int PQlibVersion();
		//int PQlibVersion(void)

		[DllImport("libpq.dll")]
		public static extern int PQisthreadsafe();
		//int PQisthreadsafe(void)

		#endregion

		//
		// http://www.postgresql.org/docs/current/static/libpq-connect.html
		//

		#region blocking connection setup

		[DllImport("libpq.dll")]
		public static extern IntPtr PQconnectdb(string conninfo);
		// PGconn *PQconnectdb(const char *conninfo)

		[DllImport("libpq.dll")]
		public static extern IntPtr PQconnectdbParams(string[] keywords, string[] values, int expand_dbname);
		// PGconn *PQconnectdbParams(const char * const *keywords, const char * const *values, int expand_dbname);

		#region non-blocking connection setup

		[DllImport("libpq.dll")]
		public static extern IntPtr PQconnectStartParams(string[] keywords, string[] values, int expand_dbname);
		// PGconn *PQconnectStartParams(const char * const *keywords, const char * const *values, int expand_dbname);

		[DllImport("libpq.dll")]
		public static extern IntPtr PQconnectStart(string conninfo);
		// PGconn *PQconnectStart(const char *conninfo);

		[DllImport("libpq.dll")]
		public static extern int PQconnectPoll(IntPtr conn);
    // PostgresPollingStatusType PQconnectPoll(PGconn *conn);

		#endregion


		#region connection settings

		[DllImport("libpq.dll")]
		public static extern int PQsetSingleRowMode(IntPtr conn);
		// int PQsetSingleRowMode(PGconn *conn);

		[DllImport("libpq.dll")]
		public static extern int PQclientEncoding(IntPtr conn);
		// int PQclientEncoding(const PGconn *conn);

		#endregion


		#region connection cleanup

		[DllImport("libpq.dll")]
		public static extern void PQfinish(IntPtr conn);
		// void PQfinish(PGconn *conn)

		#endregion

		//
		// http://www.postgresql.org/docs/current/static/libpq-status.html
		//

		#region connection status and error message

		[DllImport("libpq.dll")]
		public static extern int PQstatus(IntPtr conn);
		// ConnStatusType PQstatus(conn)

		[DllImport("libpq.dll")]
		public static extern string PQerrorMessage(IntPtr conn);
		// char *PQerrorMessage(const PGconn *conn);

		#endregion

		
		#region transaction status

		[DllImport("libpq.dll")]
		public static extern int PQtransactionStatus(IntPtr conn);
		// PGTransactionStatusType PQtransactionStatus(const PGconn *conn);

		#endregion


		#region connection settings

		[DllImport("libpq.dll")]
		public static extern int PQbackendPID(IntPtr conn);
		// int PQbackendPID(const PGconn *conn);

		[DllImport("libpq.dll")]
		public static extern int PQserverVersion(IntPtr conn);
		// int PQserverVersion(const PGconn *conn);

		[DllImport("libpq.dll")]
		public static extern string PQparameterStatus(IntPtr conn, string paramName);
		// const char *PQparameterStatus(const PGconn *conn, const char *paramName);

		#endregion

		//
		// http://www.postgresql.org/docs/current/static/libpq-exec.html
		// http://www.postgresql.org/docs/current/static/libpq-async.html
		//

		#region blocking queries

		[DllImport("libpq.dll")]
		public static extern IntPtr PQexec(IntPtr conn, string query);
		// PGresult *PQexec(PGconn *conn, const char *query);

		[DllImport("libpq.dll")]
		public static extern IntPtr PQexecParams(IntPtr conn, string command, int nParams, int[] paramTypes, IntPtr[] paramValues, int[] paramLengths, int[] paramFormats, int resultFormat);
		// PGresult *PQexecParams(PGconn *conn, const char *command, int nParams, const Oid *paramTypes, const char * const *paramValues, const int *paramLengths, const int *paramFormats, int resultFormat);

		#endregion


		#region non-blocking queries

		[DllImport("libpq.dll")]
		public static extern int PQsendQuery(IntPtr conn, string command);
		// int PQsendQuery(PGconn *conn, const char *command);

		[DllImport("libpq.dll")]
		public static extern int PQsendQueryParams(IntPtr conn, string command, int nParams, int[] paramTypes, IntPtr[] paramValues, int[] paramLengths, int[] paramFormats, int resultFormat);
		// int PQsendQueryParams(PGconn *conn, const char *command, int nParams, const Oid *paramTypes, const char * const *paramValues, const int *paramLengths, const int *paramFormats, int resultFormat);

		[DllImport("libpq.dll")]
		public static extern IntPtr PQgetResult(IntPtr conn);
		// PGresult *PQgetResult(PGconn *conn)

		#endregion


		#region result cleanup

		[DllImport("libpq.dll")]
		public static extern void PQclear(IntPtr res);
		// void PQclear(PGresult *res);

		#endregion


		#region number of rows and columns

		[DllImport("libpq.dll")]
		public static extern int PQntuples(IntPtr res);
		// int PQntuples(const PGresult *res);

		[DllImport("libpq.dll")]
		public static extern int PQnfields(IntPtr res);
		// int PQnfields(const PGresult *res);

		#endregion


		#region field type and size information

		[DllImport("libpq.dll")]
		public static extern int PQfformat(IntPtr res, int column_number);
		// int PQfformat(const PGresult *res, int column_number);

		[DllImport("libpq.dll")]
		public static extern int PQftype(IntPtr res, int column_number);
		// Oid PQftype(const PGresult *res, int column_number);

		[DllImport("libpq.dll")]
		public static extern int PQfmod(IntPtr res, int column_number);
		// int PQfmod(const PGresult *res, int column_number);

		[DllImport("libpq.dll")]
		public static extern int PQfsize(IntPtr res, int column_number);
		// int PQfsize(const PGresult *res, int column_number);

		#endregion


		#region value access of specified row,column

		[DllImport("libpq.dll")]
		public static extern IntPtr PQgetvalue(IntPtr res, int row_number, int column_number);
		// char *PQgetvalue(const PGresult *res, int row_number, int column_number);

		[DllImport("libpq.dll")]
		public static extern int PQgetisnull(IntPtr res, int row_number, int column_number);
		// int PQgetisnull(const PGresult *res, int row_number, int column_number);

		[DllImport("libpq.dll")]
		public static extern int PQgetlength(IntPtr res, int row_number, int column_number);
		// int PQgetlength(const PGresult *res, int row_number, int column_number);

		#endregion



		//
		// http://www.postgresql.org/docs/current/static/libpq-copy.html
		//



		//
		// http://www.postgresql.org/docs/current/static/largeobjects.html
		//
	}
}
