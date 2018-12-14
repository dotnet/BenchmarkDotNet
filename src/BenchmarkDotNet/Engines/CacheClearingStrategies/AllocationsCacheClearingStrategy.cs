using System;
using System.Threading;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal class AllocationsCacheClearingStrategyForOneCore : ICacheClearingStrategy
    {
        private readonly IMemoryAllocator memoryAllocator;

        public AllocationsCacheClearingStrategyForOneCore(IMemoryAllocator memoryAllocator) => this.memoryAllocator = memoryAllocator;

        public void ClearCache(IntPtr? affinity)
        {
            const int howManyProcessOnProcessor = 2;

            var threads = new Thread[howManyProcessOnProcessor];

            int index = 0;
            for (int i = 0; i < howManyProcessOnProcessor; i++)
            {
                var thread = new Thread(() => memoryAllocator.AllocateMemory()) { IsBackground = true, };
                thread.Start();

                threads[index++] = thread;
            }

            foreach (var thread in threads)
                thread.Join();
        }
    }
}