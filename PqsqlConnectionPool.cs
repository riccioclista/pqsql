using System;
using System.Collections.Generic;
using System.Threading;

namespace Pqsql
{
	internal static class PqsqlConnectionPool
	{
		// to send when releasing a connection
		private static byte[] DiscardAllStatement = PqsqlUTF8Statement.CreateUTF8Statement("DISCARD ALL");

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

		private static object mPooledConnsLock = new object();

		// 30 sec idle timeout
		const int IdleTimeout = 30000;
		// only pool connections if we have <= 50 connections in the pool
		const int MaxQueue = 50;
		// only cleanup connections if the oldest one had been visited more than VisitedThreshold times
		const int VisitedThreshold = 2;

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
			List<IntPtr> closeConnections = o as List<IntPtr>;

			// we assume that we run PoolService in less than IdleTimeout msecs
			lock (mPooledConnsLock)
			{
#if PQSQL_DEBUG
				mLogger.Debug("Running PoolService");
#endif
				Dictionary<PqsqlConnectionStringBuilder, Queue<ConnectionInfo>>.Enumerator e = mPooledConns.GetEnumerator();

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

						int maxRelease = count / 2 + 1;

						ConnectionInfo i = queue.Peek();
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
								closeConnections.Add(i.pgconn); // close connections outside of queue lock
								maxRelease--;
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
					pgConn = i.pgconn;

					if (count > 1)
					{
						// head of queue will inherit old visited count
						ConnectionInfo j = queue.Peek();
						j.visited = i.visited;
					}
				}
			}

			if (pgConn == IntPtr.Zero || !CheckOrRelease(pgConn))
			{
				pgConn = SetupPGConn(connStringBuilder);
			}

			return pgConn;
		}

		private static bool CheckOrRelease(IntPtr pgConn)
		{
			ConnectionStatus s = (ConnectionStatus) PqsqlWrapper.PQstatus(pgConn);
			if (s != ConnectionStatus.CONNECTION_OK) goto broken;

			PGTransactionStatus ts = (PGTransactionStatus) PqsqlWrapper.PQtransactionStatus(pgConn);
			if (ts != PGTransactionStatus.PQTRANS_IDLE) goto broken;

			return true;

		broken:
			PqsqlWrapper.PQfinish(pgConn);
			return false;
		}

		public static void ReleasePGConn(PqsqlConnectionStringBuilder connStringBuilder, IntPtr pgConnHandle)
		{
			Queue<ConnectionInfo> queue;

			if (pgConnHandle == IntPtr.Zero)
				return;

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
			bool rollback = false;

			ConnectionStatus cs = (ConnectionStatus) PqsqlWrapper.PQstatus(conn);
			if (cs != ConnectionStatus.CONNECTION_OK)
				return false; // connection broken

			switch ((PGTransactionStatus) PqsqlWrapper.PQtransactionStatus(conn))
			{
				case PGTransactionStatus.PQTRANS_INERROR: /* idle, within failed transaction */
				case PGTransactionStatus.PQTRANS_INTRANS: /* idle, within transaction block */
					rollback = true;
					break;

				case PGTransactionStatus.PQTRANS_IDLE: /* connection idle */
					// nothing to do
					break;

			  // PGTransactionStatus.PQTRANS_ACTIVE: /* command in progress */
				// PGTransactionStatus.PQTRANS_UNKNOWN: /* cannot determine status */
				default:
					return false; // connection broken
			}

			IntPtr res;
			ExecStatus s = ExecStatus.PGRES_FATAL_ERROR;
			
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
							s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);
							PqsqlWrapper.PQclear(res);
						}
					}
				}

				if (s != ExecStatus.PGRES_COMMAND_OK)
				{
					return false; // connection broken
				}

				s = ExecStatus.PGRES_FATAL_ERROR;
			}

			// discard connection
			unsafe
			{
				fixed (byte* st = DiscardAllStatement)
				{
					res = PqsqlWrapper.PQexec(conn, st);

					if (res != IntPtr.Zero)
					{
						s = (ExecStatus) PqsqlWrapper.PQresultStatus(res);
						PqsqlWrapper.PQclear(res);
					}
				}
			}

			if (s != ExecStatus.PGRES_COMMAND_OK)
			{
				return false; // connection broken
			}

			return true; // connection successfully resetted
		}

	}
}
