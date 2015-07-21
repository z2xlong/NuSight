using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace ConsoleTest
{
    public static class CodeTimer
    {
        static CodeTimer()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Action prewarm = () => { };
            Time("", 1, prewarm); // 对Time方法预热，以便JIT将IL编译成本地代码
        }

        // public delegate void ActionDelegate();

        public static long Time(string name, int iteration, Action action)
        {
            // Record the latest GC counts

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            int[] gcCounts = new int[GC.MaxGeneration + 1];
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                gcCounts[i] = GC.CollectionCount(i);
            }

            // Run action

            Stopwatch watch = new Stopwatch();
            watch.Start();
            long cycleCount = GetCurrentThreadTimes();
            for (int i = 0; i < iteration; i++)
            {
                action();
            }
            long cpuCycles = GetCurrentThreadTimes() - cycleCount;
            watch.Stop();

            // Print CPU

            Console.WriteLine("\tCPU Cycles:\t" + cpuCycles.ToString("N0"));

            // 5. Print GC
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                int count = GC.CollectionCount(i) - gcCounts[i];
                Console.WriteLine("\tGen " + i + ": \t\t" + count);
            }

            return watch.ElapsedMilliseconds;
        }

        private static long GetCurrentThreadTimes()
        {
            long l;
            long kernelTime, userTimer;
            GetThreadTimes(GetCurrentThread(), out l, out l, out kernelTime,out userTimer);
            return kernelTime + userTimer;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetThreadTimes(IntPtr hThread, out long lpCreationTime,
           out long lpExitTime, out long lpKernelTime, out long lpUserTime);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();
    }
}
