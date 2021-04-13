using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pqsql;
using static Pqsql.UnsafeNativeMethods;

namespace PqsqlTests
{
    [TestClass]
    public class PqsqlUtilsTest
    {
		private const long UsecsPerHour = 3600000000;
		private const long UsecsPerMinute = 60000000;
		private const long UsecsPerSecond = 1000000;

        [TestMethod]
        public unsafe void SwapBytesTest1()
        {
            const int len = 100000000;

            // create int array
            var rnd = new Random();
            var arr = Enumerable.Repeat(rnd.Next(int.MinValue, int.MaxValue), len).ToArray();

            var managedResults = new int[len];
            var unmanagedResults = new int[len];

            var sw = new Stopwatch();

            sw.Start();
            for (int i = 0; i < len; i++)
            {
                managedResults[i] = PqsqlUtils.SwapBytes(arr[i]);
            }

            var managedTime = sw.Elapsed;

            sw.Restart();
            fixed (void* p = arr)
            fixed (void* p2 = unmanagedResults)
            {
                var ptr = new IntPtr(p);
                var ptrEnd = ptr + sizeof(int) * len;
                var ptr2 = (int*) p2;
                while (ptr != ptrEnd)
                {
                    *ptr2 = PqsqlBinaryFormat.pqbf_get_int4(ptr);
                    ptr += 4;
                    ptr2++;
                }
            }

            var unmanagedTime = sw.Elapsed;
            sw.Stop();

            for (int i = 0; i < len; i++)
            {
                Assert.AreEqual(unmanagedResults[i], managedResults[i]);
            }

            Console.WriteLine("Managed time: {0}", managedTime);
            Console.WriteLine("Unmanaged time: {0}", unmanagedTime);

            Assert.IsTrue(managedTime < unmanagedTime);
        }

        [TestMethod]
        public unsafe void CalculateTime()
        {
            const int len = 10000000;

            // create int array
            var rnd = new Random();
            var arr = Enumerable.Repeat((long) rnd.Next(int.MaxValue), len).ToArray();

            var results1 = new int[len][];
            var results2 = new int[len][];

            var sw = new Stopwatch();

            sw.Start();
            for (int i = 0; i < len; i++)
            {
                var t = arr[i];
                var hour = (int) (t / UsecsPerHour);
                t -= hour * UsecsPerHour;
                
                var min = (int) (t / UsecsPerMinute);
                t -= min * UsecsPerMinute;

                var sec = (int) (t / UsecsPerSecond);
                t -= sec * UsecsPerSecond;

                var fsec = (int) t;

                results1[i] = new [] { hour, min, sec, fsec };
            }

            var results1Time = sw.Elapsed;

            sw.Restart();
            for (int i = 0; i < len; i++)
            {
                var t = arr[i];
                var hour = (int) (t / UsecsPerHour);
                var min = (int) ((t % UsecsPerHour) / UsecsPerMinute);
                var sec = (int) ((t % UsecsPerMinute) / UsecsPerSecond);
                var fsec = (int) (t % UsecsPerSecond);
                results2[i] = new [] { hour, min, sec, fsec };
            }

            var results2Time = sw.Elapsed;
            sw.Stop();

            for (int i = 0; i < len; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Assert.AreEqual(results1[i][j], results2[i][j]);
                }
            }

            Console.WriteLine("results1Time time: {0}", results1Time);
            Console.WriteLine("results2Time time: {0}", results2Time);

            //Assert.IsTrue(results1Time < results2Time);
        }
    }
}