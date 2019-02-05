using System;

namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal interface ICacheClearingStrategy
    {
        void ClearCache();
    }
}