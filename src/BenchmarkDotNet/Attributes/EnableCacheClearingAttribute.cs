using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class EnableCacheClearingAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public EnableCacheClearingAttribute(CacheClearingStrategy value = CacheClearingStrategy.Allocations)
        {
            Config = ManualConfig.CreateEmpty().CacheClearingStrategy(value);
        }
    }
}