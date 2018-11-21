using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal class AllocationsCacheClearingStrategy : ICacheClearingStrategy
    {
        public void ClearCache()
        {
            var howManyProcessOnCore = 2;
            
            //Get the processor count of our machine.
            int cpuCount = Environment.ProcessorCount;
            Console.WriteLine("cpuCount " + cpuCount);

            //Get the our application's process.
//            Process process = Process.GetCurrentProcess();
            
            //Since the application starts with a few threads, we have to record the offset.
//            var currentThread = process.Threads.Cast<ProcessThread>().Select(p => p.Id).ToArray();

//            int offset = process.Threads.Count;
//            Console.WriteLine("offset " + offset);
            Thread[] threads = new Thread[cpuCount * howManyProcessOnCore];

            //Create and start a number of threads.

            Console.WriteLine("cpuCount * howManyProcessOnCore " + cpuCount * howManyProcessOnCore);

            for (int i = 0; i < cpuCount * howManyProcessOnCore; ++i)
            {
                Console.WriteLine("new "+ i);
                var t = new Thread(()=>AllocateMemory(1 << (i % cpuCount))) { IsBackground = true, };
                t.Start();

                threads[i] = t;
            }

            // Refresh the process information in order to get the newest thread list.
//            process.Refresh();
//            var newThread = process.Threads.Cast<ProcessThread>().Where(p=> currentThread.All(c => c != p.Id)).ToDictionary(p => p.Id, p => p);
//            Console.WriteLine("newThread.Count " + newThread.Count);
//            Console.WriteLine("new offset " + process.Threads.Count);

            //Set the affinity of newly created threads.
//            for (int i = 0; i < cpuCount * howManyProcessOnCore; ++i)
//            {
//                if (newThread.Count > i) //check if process is still running.
//                {
//                    Console.WriteLine("(i % cpuCount) " + (i % cpuCount));
                    
//                    process.Threads[i + offset].ProcessorAffinity = (IntPtr)(1L << (i % cpuCount));
//                }
//            }

            //Wait for all thread
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        public static void SetProcessorAffinity(int coreMask)
        {
            int threadId = GetCurrentThreadId();
            Console.WriteLine("threadId " + threadId + " coreMask "+ coreMask);
            SafeThreadHandle handle = null;
            var tempHandle = new object();
            try
            {
                handle = OpenThread(0x60, false, threadId);
                if (SetThreadAffinityMask(handle, new HandleRef(tempHandle, (IntPtr)coreMask)) == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to set processor affinity for thread");
                }
                else
                {
                    Console.WriteLine("OK");
                }
            }
            finally
            {
                if (handle != null)
                {
                    handle.Close();
                }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetThreadAffinityMask(SafeThreadHandle handle, HandleRef mask);

        [SuppressUnmanagedCodeSecurity]
        public class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeThreadHandle() : base(true)
            {

            }

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32")]
        public static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeThreadHandle OpenThread(int access, bool inherit, int threadId);


        public void AllocateMemory(int affinity)
        {
            SetProcessorAffinity(affinity);

            
            var HowManyMB = 6;
            var HowManyPass = 10;
            var SizeOfOneArray = 64;
            for (int i = 0; i < (HowManyMB * 1024 * 1024 / SizeOfOneArray) * HowManyPass; i++)
            {
                var tmpArray = new int[SizeOfOneArray];

                for (int j = 0; j < SizeOfOneArray; j++)
                {
                    tmpArray[j] = i * j;
                }

                tmpArray[0] = 6;

                for (int j = 0; j < SizeOfOneArray; j++)
                {
                    Consumer(tmpArray[j]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Consumer(int v)
        {

        }
    }
}