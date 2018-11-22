using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
//    [RPlotExporter] 
//    [EnableCacheClearing(CacheClearingStrategy.Allocations)] //or --CacheClearingStrategy Allocations
    [Config(typeof(StatisticColumnConfig))]
    public class IntroColdCpuCache
    {
        private class StatisticColumnConfig : ManualConfig
        {
            public StatisticColumnConfig()
            {
                Add(StatisticColumn.Q1,
                    StatisticColumn.Median,
                    StatisticColumn.Q3,
                    StatisticColumn.Skewness,
                    StatisticColumn.Kurtosis);
            }
        }

        private readonly int[] array0 = Enumerable.Range(1, 1024 * 1024).ToArray();

        [Benchmark]
        public int AllocationsStrategy()
        {
            return ArraySum(array0); 
        }

        private static int ArraySum(int[] array)
        {
            int result = 0;
            for (int i = 0; i < array.Length; i++)
            {
                result += array[i];
            }

            return result;
        }
    }
}
