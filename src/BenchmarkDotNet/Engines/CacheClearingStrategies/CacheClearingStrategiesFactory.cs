using System;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal static class CacheClearingStrategiesFactory
    {
        public static ICacheClearingStrategy GetStrategy(CacheClearingStrategy cacheClearingStrategy)
        {
            if (!RuntimeInformation.IsWindows())
            {
                //All methods use pinvoke.
                return null;
            }

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
