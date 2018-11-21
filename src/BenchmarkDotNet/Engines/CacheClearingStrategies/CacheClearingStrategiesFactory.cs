using System;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal static class CacheClearingStrategiesFactory
    {
        public static ICacheClearingStrategy GetStrategy(CacheClearingStrategy cacheClearingStrategy)
        {
            switch (cacheClearingStrategy)
            {
                case CacheClearingStrategy.None:
                    return null;
                case CacheClearingStrategy.Allocations:
                    return new AllocationsCacheClearingStrategy();
                case CacheClearingStrategy.Native:
                    return new NativeCacheClearingStrategy();
                default:
                    throw new NotSupportedException($"Not supported cache clearing strategy: {cacheClearingStrategy}.");
            }
        }
    }
}
