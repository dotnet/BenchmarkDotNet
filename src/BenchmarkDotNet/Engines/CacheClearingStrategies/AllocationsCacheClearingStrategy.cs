using System;
using System.Threading;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal class AllocationsCacheClearingStrategyForOneCore : ICacheClearingStrategy
    {
        private readonly ICacheMemoryCleaner cacheMemoryCleaner;

        public AllocationsCacheClearingStrategyForOneCore(ICacheMemoryCleaner cacheMemoryCleaner) => this.cacheMemoryCleaner = cacheMemoryCleaner;

        public void ClearCache()
        {
            const int howManyProcessOnProcessor = 3;

            var threads = new Thread[howManyProcessOnProcessor];

            for (int i = 0; i < howManyProcessOnProcessor; i++)
            {
                var thread = new Thread(() => cacheMemoryCleaner.Clean()) { IsBackground = true, };
                thread.Start();

                threads[i] = thread;
            }

            foreach (var thread in threads)
                thread.Join();
        }
    }
}