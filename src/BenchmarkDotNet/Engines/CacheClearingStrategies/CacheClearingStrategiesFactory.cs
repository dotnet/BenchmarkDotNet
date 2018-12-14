using System;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal static class CacheClearingStrategiesFactory
    {
        public static ICacheClearingStrategy GetStrategy(CacheClearingStrategy cacheClearingStrategy, IntPtr? affinity)
        {
            if (cacheClearingStrategy == CacheClearingStrategy.None)
                return null;

            if (cacheClearingStrategy == CacheClearingStrategy.Native)
            {
                return new NativeCacheClearingStrategy();
            }

            if (cacheClearingStrategy == CacheClearingStrategy.Allocations)
            {
                if (affinity.HasValue && CountSetBits((int) affinity.Value) <= 1)
                    return new AllocationsCacheClearingStrategyForOneCore(new MemoryAllocator());

                return new AllocationsCacheClearingStrategyForWindows(new MemoryAllocator());
            }

            return null;
        }

        public static string Validate(CacheClearingStrategy cacheClearingStrategy, int? affinity)
        {
            if (!RuntimeInformation.IsWindows())
            {
                if (cacheClearingStrategy == CacheClearingStrategy.Native)
                {
                    return "The native method of clearing the cache is only available on Windows.";
                }

                if (cacheClearingStrategy == CacheClearingStrategy.Allocations)
                {
                    if (!affinity.HasValue || CountSetBits(affinity.Value) != 1)
                        return "The allocations method of clearing the cache for more then one core is only available on Windows. Please use --affinity if you want to set process affinity.";
     
                }
            }

            return null;
        }

        private static int CountSetBits(int n)
        {
            int count = 0;
            while (n > 0)
            {
                count += n & 1;
                n >>= 1;
            }

            return count;
        }
    }
}