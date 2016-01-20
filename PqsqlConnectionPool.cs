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
		// global timer for cleaning connections
		private static readonly Timer mTimer = new Timer(PoolService, null, Timeout.Infinite, Timeout.Infinite);

		// maps connection strings to connection queues
		private static readonly Dictionary<PqsqlConnectionStringBuilder, Queue<ConnectionInfo>> mPooledConns = new Dictionary<PqsqlConnectionStringBuilder, Queue<ConnectionInfo>>();

		private static object mPooledConnsLock = new object();

		// 30 sec idle timeout
		private static readonly TimeSpan IdleTimeout = new TimeSpan(0,0,30);
		// only pool connections if we have <= 50 connections in the pool
		const int MaxQueue = 50;


		private static void RestartTimer()
		{
			lock (mPooledConnsLock)
			{
				mTimer.Change(IdleTimeout.Seconds * 1000, Timeout.Infinite);
			}
		}


		private static void PoolService(object o)
		{
			DateTime now = DateTime.Now;
			bool haveConnections = false;

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
								PqsqlWrapper.PQfinish(i.Item1); // close connection and release memory
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
			IntPtr pgConn;

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
				else
				{
					pgConn = SetupPGConn(connStringBuilder);
				}
			}

			RestartTimer();

			return pgConn;
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

			if (queue == null)
			{
				PqsqlWrapper.PQfinish(pgConnHandle); // close connection and release memory
				return; // Queue may be emptied by connection problems. See ClearPool below.
			}

			lock (queue)
			{
				if (queue.Count < MaxQueue)
				{
					queue.Enqueue(new ConnectionInfo(pgConnHandle, DateTime.Now));
				}
				else
				{
					PqsqlWrapper.PQfinish(pgConnHandle); // close connection and release memory
				}
			}

			RestartTimer();
		}


	}
}
