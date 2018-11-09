using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
//    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, targetCount: 3)]
    [CsvMeasurementsExporter]
    [RPlotExporter]

    public class IntroColdCpuCache
    {
        [DllImport("kernel32.dll")]
        static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress,
            UIntPtr dwSize);
        
        [IterationSetup(Target = "WithoutCash")]
        public void IterationSetup()
        {
            Process process = Process.GetCurrentProcess();
            FlushInstructionCache(process.Handle, IntPtr.Zero, UIntPtr.Zero);
        }
//        [IterationSetup(Target = "WithoutCache")]
//        public void IterationSetup()
//        {
//            var howManyProcessOnCore = 2;
//            //Get the processor count of our machine.
//            int cpuCount = Environment.ProcessorCount;
//
//            //Get the our application's process.
//            Process process = Process.GetCurrentProcess();
//
//            //Since the application starts with a few threads, we have to record the offset.
//            int offset = process.Threads.Count;
//            Thread[] threads = new Thread[cpuCount * howManyProcessOnCore];
//
//            //Create and start a number of threads.
//            for (int i = 0; i < cpuCount * howManyProcessOnCore; ++i)
//            {
//                var t = new Thread(AllocateMemory) { IsBackground = true };
//                t.Start();
//               
//                threads[i] = t;
//            }
//
//            // Refresh the process information in order to get the newest thread list.
//            process.Refresh();
//
//            //Set the affinity of newly created threads.
//            var processThreads = process.Threads;
//            for (int i = 0; i < cpuCount * howManyProcessOnCore; ++i)
//            {
//                if (processThreads.Count > i + offset) //check if process is still running.
//                {
//                    //distributes threads evenly on all processors.
//                    process.Threads[i + offset].ProcessorAffinity = (IntPtr) (1L << (i % cpuCount));
//                }
//            }
//
//            //Wait for all thread
//            foreach (var thread in threads)
//            {
//                thread.Join();
//            }
//        }
//
//        public void AllocateMemory()
//        {
//            var HowManyMB = 6;
//            var HowManyPass = 10;
//            var SizeOfOneArray = 64;
//            for (int i = 0; i < (HowManyMB * 1024 * 1024 / SizeOfOneArray) * HowManyPass; i++)
//            {
//                var tmpArray = new int[SizeOfOneArray];
//
//                for (int j = 0; j < SizeOfOneArray; j++)
//                {
//                    tmpArray[j] = i * j;
//                }
//
//                tmpArray[0] = 6;
//
//                for (int j = 0; j < SizeOfOneArray; j++)
//                {
//                    Consumer(tmpArray[j]);
//                }
//            }
//        }
//
//        [MethodImpl(MethodImplOptions.NoInlining)]
//        public void Consumer(int v)
//        {
//
//        }

        private readonly int[] array0 = Enumerable.Range(1, 1024 * 1024).ToArray();

        [Benchmark]
        public int WithoutCache()
        {
            return ArraySum(array0); 
        }

        [Benchmark]
        public int WithCache()
        {
            return ArraySum(array0);
        }

        private static int ArraySum(int[] array)
        {
            int result = 0;
            for (int i = 0; i < array.Length; i++)
            {
                result += array[i];
            }

            return result;
        }
    }

   
}
