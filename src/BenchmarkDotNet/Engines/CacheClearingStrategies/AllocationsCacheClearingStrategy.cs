using System;
using System.Collections.Generic;
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
        public void ClearCache(IntPtr? affinity)
        {
            if (affinity.HasValue)
            {
                ClearCacheForKnownAffinity(affinity.Value);
            }
            else
            {
                ClearCacheForAllProcessor();
            }            
        }

        private void ClearCacheForKnownAffinity(IntPtr affinity)
        {
            ClearCache(GetAffinitiesForSelectedProcessors(affinity).ToArray());
        }

        private static IEnumerable<int> GetAffinitiesForSelectedProcessors(IntPtr affinityPtr)
        {
            int cpuCount = Environment.ProcessorCount;

            int affinity = (int)affinityPtr;

            for (int i = 0; i < cpuCount; i++)
            {
                var cpuMask = 1 << i;

                if ((affinity & cpuMask) > 0)
                {
                    yield return cpuMask;
                }
            }
        }

        private void ClearCacheForAllProcessor()
        {
            //Get the processor count of our machine.
            int cpuCount = Environment.ProcessorCount;

            var affinities = new int[cpuCount];
            for (int i = 0; i < cpuCount ; ++i)
            {
                affinities[i] = (1 << i);

            }

            ClearCache(affinities);
        }

        private void ClearCache(int[] affinities)
        {
            const int howManyProcessOnProcessor = 2;

            var threads = new Thread[affinities.Length * howManyProcessOnProcessor];

            int index = 0;
            for (int i = 0; i < howManyProcessOnProcessor; i++)
            {
                foreach (int affinity in affinities)
                {
                    var thread = new Thread(() => AllocateMemory(affinity)) { IsBackground = true, };
                    thread.Start();

                    threads[index++] = thread;
                }
            }

            //Wait for all thread
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        private void AllocateMemory(long affinity)
        {
            SetProcessorAffinity(affinity);

            const int howManyMb = 6;
            const int howManyPass = 10;
            const int sizeOfOneArray = 64;
            for (int i = 0; i < (howManyMb * 1024 * 1024 / sizeOfOneArray) * howManyPass; i++)
            {
                var tmpArray = new int[sizeOfOneArray];

                for (int j = 0; j < sizeOfOneArray; j++)
                {
                    tmpArray[j] = i * j;
                }

                tmpArray[0] = 6;

                for (int j = 0; j < sizeOfOneArray; j++)
                {
                    Consumer(tmpArray[j]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Consumer(int v)
        {

        }

        private static void SetProcessorAffinity(long coreMask)
        {
            int threadId = GetCurrentThreadId();
            SafeThreadHandle handle = null;
            var tempHandle = new object();
            try
            {
                handle = OpenThread(0x60, false, threadId);
                if (SetThreadAffinityMask(handle, new HandleRef(tempHandle, (IntPtr)coreMask)) == IntPtr.Zero)
                {

                    Console.WriteLine("Failed to set processor affinity for thread "+ Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                handle?.Close();
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetThreadAffinityMask(SafeThreadHandle handle, HandleRef mask);

        [SuppressUnmanagedCodeSecurity]
        private class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
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
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32")]
        private static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeThreadHandle OpenThread(int access, bool inherit, int threadId);
    }
}