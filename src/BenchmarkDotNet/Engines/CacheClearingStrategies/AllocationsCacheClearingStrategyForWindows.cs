using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal class AllocationsCacheClearingStrategyForWindows : ICacheClearingStrategy
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetThreadAffinityMask(SafeThreadHandle handle, HandleRef mask);

        [SuppressUnmanagedCodeSecurity]
        private class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeThreadHandle() : base(true) { }

            protected override bool ReleaseHandle() => CloseHandle(handle);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32")]
        private static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeThreadHandle OpenThread(int access, bool inherit, int threadId);

        private readonly ICacheMemoryCleaner cacheMemoryCleaner;

        public AllocationsCacheClearingStrategyForWindows(ICacheMemoryCleaner cacheMemoryCleaner) => this.cacheMemoryCleaner = cacheMemoryCleaner;

        public void ClearCache(IntPtr? affinity)
        {
            if (affinity.HasValue)
                ClearCacheForKnownAffinity(affinity.Value);
            else
                ClearCacheForAllProcessor();
        }

        private void ClearCacheForKnownAffinity(IntPtr affinity)
        {
            ClearCache(GetAffinitiesForSelectedProcessors(affinity).ToArray());
        }

        private void ClearCacheForAllProcessor()
        {
            int cpuCount = Environment.ProcessorCount;

            var affinities = new int[cpuCount];
            for (int i = 0; i < cpuCount; ++i)
                affinities[i] = 1 << i;

            ClearCache(affinities);
        }

        private void ClearCache(int[] affinities)
        {
            const int howManyProcessOnProcessor = 2;

            var threads = new Thread[affinities.Length * howManyProcessOnProcessor];

            int index = 0;
            for (int i = 0; i < howManyProcessOnProcessor; i++)
                foreach (int affinity in affinities)
                {
                    var thread = new Thread(() => AllocateMemory(affinity)) { IsBackground = true, };
                    thread.Start();

                    threads[index++] = thread;
                }

            foreach (var thread in threads)
                thread.Join();
        }

        private void AllocateMemory(int affinity)
        {
            SetProcessorAffinity(affinity);
            cacheMemoryCleaner.Clean();
        }

        private static void SetProcessorAffinity(long coreMask)
        {
            int threadId = GetCurrentThreadId();
            SafeThreadHandle handle = null;
            var tempHandle = new object();
            try
            {
                handle = OpenThread(0x60, false, threadId);
                if (SetThreadAffinityMask(handle, new HandleRef(tempHandle, (IntPtr) coreMask)) == IntPtr.Zero)
                    Console.WriteLine("Failed to set processor affinity for thread " + Marshal.GetLastWin32Error());
            }
            finally
            {
                handle?.Close();
            }
        }

        private static IEnumerable<int> GetAffinitiesForSelectedProcessors(IntPtr affinityPtr)
        {
            int cpuCount = Environment.ProcessorCount;

            int affinity = (int)affinityPtr;

            for (int i = 0; i < cpuCount; i++)
            {
                int cpuMask = 1 << i;

                if ((affinity & cpuMask) > 0)
                    yield return cpuMask;
            }
        }
    }
}