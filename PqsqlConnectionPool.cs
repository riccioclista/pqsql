﻿using System;
using System.Collections.Generic;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif
using System.Threading;

namespace Pqsql
{
	internal static class PqsqlConnectionPool
	{
		// to send when releasing a connection
		private static readonly byte[] DiscardAllStatement = PqsqlUTF8Statement.CreateUTF8Statement("DISCARD ALL");

		// ConnectionInfo pool object
		private class ConnectionInfo
		{
			// PGConn pointer
			public IntPtr pgconn;
			// how often did PoolService visit this item
			public int visited;
		}

		// maps connection strings to connection queues
		private static readonly Dictionary<PqsqlConnectionStringBuilder, Queue<ConnectionInfo>> mPooledConns = new Dictionary<PqsqlConnectionStringBuilder, Queue<ConnectionInfo>>();

		private static readonly object mPooledConnsLock = new object();

		// 30 sec idle timeout
		const int IdleTimeout = 30000;
		// only pool connections if we have <= 50 connections in the pool
		const int MaxQueue = 50;
		// only cleanup connections if the oldest one had been visited more than VisitedThreshold times
		const int VisitedThreshold = 2;

		// busy wait 500msec until we have received the empty result
		const int EmptyBusyWait = 500;
		// maximum number of retries waiting for the empty result
		const int EmptyMaxRetry = 20;

		// global timer for cleaning connections
		private static readonly Timer mTimer;

#if PQSQL_DEBUG
		private static log4net.ILog mLogger;
#endif

		// static constructor for log4net
		static PqsqlConnectionPool()
		{
			mTimer = new Timer(PoolService, new List<IntPtr>(), Timeout.Infinite, Timeout.Infinite);
			mTimer.Change(IdleTimeout, IdleTimeout);

#if PQSQL_DEBUG
			log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout("%date{ISO8601} [%thread] %-5level %logger - %message%newline");
			log4net.Appender.RollingFileAppender appender = new log4net.Appender.RollingFileAppender
			{
				File = "c:\\windows\\temp\\pqsql_connection_pool.log",
				Layout = layout
			};
			layout.ActivateOptions();
			appender.ActivateOptions();
			log4net.Config.BasicConfigurator.Configure(appender);
			mLogger = log4net.LogManager.GetLogger(typeof(PqsqlConnectionPool));
#endif
		}

		private static void PoolService(object o)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(o != null);
#else
			if (o == null)
				throw new ArgumentNullException("o");
#endif

			List<IntPtr> closeConnections = o as List<IntPtr>;

			// we assume that we run PoolService in less than IdleTimeout msecs
			lock (mPooledConnsLock)
			{
#if PQSQL_DEBUG
				mLogger.Debug("Running PoolService");
#endif

				using (Dictionary<PqsqlConnectionStringBuilder, Queue<ConnectionInfo>>.Enumerator e = mPooledConns.GetEnumerator())
				{
					while (e.MoveNext())
					{
						KeyValuePair<PqsqlConnectionStringBuilder, Queue<ConnectionInfo>> item = e.Current;
#if PQSQL_DEBUG
					PqsqlConnectionStringBuilder csb = item.Key;
#endif
						Queue<ConnectionInfo> queue = item.Value;

						lock (queue)
						{
							int count = queue.Count;

#if PQSQL_DEBUG
						mLogger.DebugFormat("ConnectionPool {0}: {1} waiting connections", csb.ConnectionString, count);
#endif

							if (count == 0) continue;

							int maxRelease = count/2 + 1;

							ConnectionInfo i = queue.Peek();
#if CODECONTRACTS
							Contract.Assume(i != null);
#endif
							i.visited++;

#if PQSQL_DEBUG
						if (i.visited <= VisitedThreshold)
						{
							mLogger.DebugFormat("ConnectionPool {0}: {1} visits", csb.ConnectionString, i.visited);
						}
#endif

							if (i.visited > VisitedThreshold)
							{
#if PQSQL_DEBUG
							mLogger.DebugFormat("ConnectionPool {0}: visit threshold {1} reached, releasing {2} connections", csb.ConnectionString, i.visited, maxRelease);
#endif
								while (maxRelease > 0)
								{
									// clean maxRelease connections
									i = queue.Dequeue();
#if CODECONTRACTS
									Contract.Assume(i != null);
#endif
									closeConnections.Add(i.pgconn); // close connections outside of queue lock
									maxRelease--;
								}
							}
						}
					}
				}
			}

			// now close old connections
			foreach (IntPtr conn in closeConnections)
			{
				PqsqlWrapper.PQfinish(conn); // close connection and release memory
			}

			closeConnections.Clear();
		}


		internal static IntPtr SetupPGConn(PqsqlConnectionStringBuilder connStringBuilder)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(connStringBuilder != null);
#else
			if (connStringBuilder == null)
				throw new ArgumentNullException("connStringBuilder");
#endif

			// setup null-terminated key-value arrays for the connection
			string[] keys = new string[connStringBuilder.Keys.Count + 1];
			string[] vals = new string[connStringBuilder.Values.Count + 1];

			// get keys and values from PqsqlConnectionStringBuilder
			connStringBuilder.Keys.CopyTo(keys, 0);
			connStringBuilder.Values.CopyTo(vals, 0);

			// now create connection
			return PqsqlWrapper.PQconnectdbParams(keys, vals, 0);
		}


		public static IntPtr GetPGConn(PqsqlConnectionStringBuilder connStringBuilder)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(connStringBuilder != null);
#else
			if (connStringBuilder == null)
				throw new ArgumentNullException("connStringBuilder");
#endif

			Queue<ConnectionInfo> queue;
			IntPtr pgConn = IntPtr.Zero;

			lock (mPooledConnsLock)
			{
				if (!mPooledConns.TryGetValue(connStringBuilder, out queue))
				{
					queue = new Queue<ConnectionInfo>();
					mPooledConns[connStringBuilder] = queue;
				}
			}

			lock (queue)
			{
				int count = queue.Count;
				if (count > 0)
				{
					ConnectionInfo i = queue.Dequeue();
#if CODECONTRACTS
					Contract.Assume(i != null);
#endif
					pgConn = i.pgconn;

					if (count > 1)
					{
						// head of queue will inherit old visited count
						ConnectionInfo j = queue.Peek();
#if CODECONTRACTS
						Contract.Assume(j != null);
#endif
						j.visited = i.visited;
					}
				}
			}

			if (!CheckOrRelease(pgConn))
			{
				pgConn = SetupPGConn(connStringBuilder);
			}

			return pgConn;
		}

		private static bool CheckOrRelease(IntPtr pgConn)
		{
			if (pgConn == IntPtr.Zero)
				return false;

			// is connection reusable?
			ConnStatusType s = PqsqlWrapper.PQstatus(pgConn);
			if (s != ConnStatusType.CONNECTION_OK) goto broken;

			PGTransactionStatusType ts = PqsqlWrapper.PQtransactionStatus(pgConn);
			if (ts != PGTransactionStatusType.PQTRANS_IDLE) goto broken;

			// send empty query to test whether we are really connected (tcp keepalive might have closed socket)
			unsafe
			{
				byte[] empty = { 0 }; // empty query string

				fixed (byte* eq = empty)
				{
					if (PqsqlWrapper.PQsendQuery(pgConn, eq) == 0) // could not send query
						goto broken;
				}
			}

			// wait for empty result: after sending query, we might have to wait for the result for too long
			// (network device down or server has just died)
			int retry = 0;
			while (retry < EmptyMaxRetry)
			{
				if (PqsqlWrapper.PQconsumeInput(pgConn) == 0)
					goto broken;

				if (PqsqlWrapper.PQisBusy(pgConn) == 1) // PQgetResult will block 
				{
					retry++;
					Thread.Sleep(EmptyBusyWait);
				}
				else // done receiving empty result, PQgetResult will not block 
				{
					break;
				}
			}

			if (retry >= EmptyMaxRetry) // timeout reading empty result
				goto broken;
			
			// Reading empty result: consume and clear remaining results until we reach the NULL result
			IntPtr res;
			ExecStatusType st = ExecStatusType.PGRES_EMPTY_QUERY;
			while ((res = PqsqlWrapper.PQgetResult(pgConn)) != IntPtr.Zero)
			{
				ExecStatusType st0 = PqsqlWrapper.PQresultStatus(res);

				if (st0 != ExecStatusType.PGRES_EMPTY_QUERY)
					st = st0;

				// always free res
				PqsqlWrapper.PQclear(res);
			}

			if (st != ExecStatusType.PGRES_EMPTY_QUERY) // received wrong exec status
				goto broken;

			return true; // successfully reused connection

		broken:
			// reconnect with current connection setting
			PqsqlWrapper.PQreset(pgConn);

			s = PqsqlWrapper.PQstatus(pgConn);
			if (s == ConnStatusType.CONNECTION_OK)
			{
				ts = PqsqlWrapper.PQtransactionStatus(pgConn);
				if (ts == PGTransactionStatusType.PQTRANS_IDLE)
					return true; // successfully reconnected
			}

			// could not reconnect: finally give up and clean up memory
			PqsqlWrapper.PQfinish(pgConn);
			return false;
		}

		public static void ReleasePGConn(PqsqlConnectionStringBuilder connStringBuilder, IntPtr pgConnHandle)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(connStringBuilder != null);
#else
			if (connStringBuilder == null)
				throw new ArgumentNullException("connStringBuilder");
#endif

			if (pgConnHandle == IntPtr.Zero)
				return;

			Queue<ConnectionInfo> queue;

			lock (mPooledConnsLock)
			{
				mPooledConns.TryGetValue(connStringBuilder, out queue);
			}

			bool closeConnection = true;

			if (queue == null || !DiscardConnection(pgConnHandle))
			{
				goto close; // just cleanup connection and restart timer
			}

			lock (queue)
			{
				if (queue.Count < MaxQueue)
				{
					queue.Enqueue(new ConnectionInfo{ pgconn = pgConnHandle, visited = 0 });
					closeConnection = false; // keep connection
				}
			}

		close:
			if (closeConnection)
			{
				PqsqlWrapper.PQfinish(pgConnHandle); // close connection and release memory
			}
		}


		private static bool DiscardConnection(IntPtr conn)
		{
			if (conn == IntPtr.Zero)
				return false;

			bool rollback = false;

			ConnStatusType cs = PqsqlWrapper.PQstatus(conn);
			if (cs != ConnStatusType.CONNECTION_OK)
				return false; // connection broken

			PGTransactionStatusType ts = PqsqlWrapper.PQtransactionStatus(conn);
			switch (ts)
			{
				case PGTransactionStatusType.PQTRANS_INERROR: /* idle, within failed transaction */
				case PGTransactionStatusType.PQTRANS_INTRANS: /* idle, within transaction block */
					rollback = true;
					break;

				case PGTransactionStatusType.PQTRANS_IDLE: /* connection idle */
					// nothing to do
					break;

			  // PGTransactionStatusType.PQTRANS_ACTIVE: /* command in progress */
				// PGTransactionStatusType.PQTRANS_UNKNOWN: /* cannot determine status */
				default:
					return false; // connection broken
			}

			IntPtr res;
			ExecStatusType s = ExecStatusType.PGRES_FATAL_ERROR;
			
			// we need to rollback before we can discard the connection
			if (rollback)
			{
				unsafe
				{
					fixed (byte* st = PqsqlTransaction.RollbackStatement)
					{
						res = PqsqlWrapper.PQexec(conn, st);

						if (res != IntPtr.Zero)
						{
							s = PqsqlWrapper.PQresultStatus(res);
							PqsqlWrapper.PQclear(res);
						}
					}
				}

				if (s != ExecStatusType.PGRES_COMMAND_OK)
				{
					return false; // connection broken
				}

				s = ExecStatusType.PGRES_FATAL_ERROR;
			}

			// discard connection
			unsafe
			{
				fixed (byte* st = DiscardAllStatement)
				{
					res = PqsqlWrapper.PQexec(conn, st);

					if (res != IntPtr.Zero)
					{
						s = PqsqlWrapper.PQresultStatus(res);
						PqsqlWrapper.PQclear(res);
					}
				}
			}

			if (s != ExecStatusType.PGRES_COMMAND_OK)
			{
				return false; // connection broken
			}

			return true; // connection successfully resetted
		}

	}
}
