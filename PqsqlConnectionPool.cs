using System;
using System.Collections.Generic;
using System.Threading;

// declare ConnectionInfo pool object
//
// Item1: PGConn pointer
// Item2: time when connection entered the connection pool
using ConnectionInfo = System.Tuple<System.IntPtr, System.DateTime>;

namespace Pqsql
{
	internal static class PqsqlConnectionPool
	{
		// to send when releasing a connection
		private static byte[] DiscardAllStatement = PqsqlUTF8Statement.CreateUTF8Statement("DISCARD ALL");


		// maps connection strings to connection queues
		private static readonly Dictionary<PqsqlConnectionStringBuilder, Queue<ConnectionInfo>> mPooledConns = new Dictionary<PqsqlConnectionStringBuilder, Queue<ConnectionInfo>>();

		private static object mPooledConnsLock = new object();

		// 30 sec idle timeout
		private static readonly TimeSpan IdleTimeout = new TimeSpan(0,0,30);
		// only pool connections if we have <= 50 connections in the pool
		const int MaxQueue = 50;

		// global timer for cleaning connections
		private static readonly Timer mTimer = new Timer(PoolService, null, Timeout.Infinite, Timeout.Infinite);

		private static void RestartTimer()
		{
			mTimer.Change(IdleTimeout.Seconds * 1000, Timeout.Infinite);
		}


		private static void PoolService(object o)
		{
			DateTime now = DateTime.Now;
			bool haveConnections = false;
			List<IntPtr> closeConnections = new List<IntPtr>();

			lock (mPooledConnsLock)
			{
				foreach (Queue<ConnectionInfo> queue in mPooledConns.Values)
				{
					lock (queue)
					{
						int count = queue.Count;
						if (count == 0) continue;

						haveConnections = true;
						int maxRelease = count / 2 + 1;

						while (maxRelease > 0)
						{
							ConnectionInfo i = queue.Peek();

							if (now - i.Item2 > IdleTimeout)
							{
								i = queue.Dequeue();
								closeConnections.Add(i.Item1); // close connections outside of queue lock
								maxRelease--;
							}
							else
							{
								maxRelease = 0;
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

			if (haveConnections)
			{
				RestartTimer();
			}
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
				if (queue.Count > 0)
				{
					ConnectionInfo i = queue.Dequeue();
					pgConn = i.Item1;
				}
			}

			if (pgConn == IntPtr.Zero || !CheckOrRelease(pgConn))
			{
				pgConn = SetupPGConn(connStringBuilder);
			}

			RestartTimer();

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

			if (queue == null || !DiscardConnection(pgConnHandle))
			{
				PqsqlWrapper.PQfinish(pgConnHandle); // close connection and release memory
				return;
			}

			bool closeConnection = true;
			lock (queue)
			{
				if (queue.Count < MaxQueue)
				{
					queue.Enqueue(new ConnectionInfo(pgConnHandle, DateTime.Now));
					closeConnection = false;
				}
			}

			if (closeConnection)
			{
				PqsqlWrapper.PQfinish(pgConnHandle); // close connection and release memory
			}

			RestartTimer();
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
