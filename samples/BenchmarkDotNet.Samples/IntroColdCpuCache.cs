using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(CompereCacheClearingStrategies))]
    public class IntroColdCpuCache
    {
        private class CompereCacheClearingStrategies : ManualConfig
        {
            public CompereCacheClearingStrategies()
            {
                Add(Job.Default.WithCacheClearingStrategy(CacheClearingStrategy.Allocations));
                Add(Job.Default.WithCacheClearingStrategy(CacheClearingStrategy.None));
                Add(Job.Default.WithAffinity((IntPtr) 1).WithCacheClearingStrategy(CacheClearingStrategy.Allocations));
                Add(Job.Default.WithAffinity((IntPtr) 1).WithCacheClearingStrategy(CacheClearingStrategy.None));

                Add(StatisticColumn.Q1,
                    StatisticColumn.Median,
                    StatisticColumn.Q3,
                    StatisticColumn.Skewness,
                    StatisticColumn.Kurtosis);
            }
        }

        private readonly int[] array = Enumerable.Range(1, 1024 * 1024 * 1024).ToArray();

        [Benchmark]
        public int ArraySum()
        {
            int result = 0;
            for (int i = 0; i < array.Length; i += 16)
            {
                result += array[i];
            }

            return result;
        }
    }
}
