using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics.StatisticalTesting;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(CompareCacheClearingStrategies))]
    public class IntroColdCpuCache
    {
        private class CompareCacheClearingStrategies : ManualConfig
        {
            public CompareCacheClearingStrategies()
            {
                Add(Job.ShortRun.WithIterationTime(TimeInterval.FromMilliseconds(10)).AsBaseline());

                Add(Job.ShortRun.WithCleanCache().WithIterationTime(TimeInterval.FromMilliseconds(10)));

                Add(MemoryDiagnoser.Default);
                Add(HardwareCounter.CacheMisses);
            }
        }

        private const int arrayCount = 10 * 1024; // 10 KB
        private readonly int[] array = Enumerable.Repeat(1, arrayCount).ToArray();

        [Benchmark(Description = "Clean Cache affect performance, Benchmark doesn't make allocation.")]
        public int CleanCacheAffectsPerformance()
        {
            int result = 0;
            for (int i = 0; i < arrayCount; i += 16) // 16 because sizeof(int) = 4, 4 * 16 = 64 bytes.
            {
                result += array[i];
            }

            return result;
        }

        [Benchmark(Description = "Clean Cache doesn't affect performance, Benchmark make allocation.")]
        public int CleanCacheDoesNotAffectsPerformanceWithAllocationInBenchmark()
        {
            const int arrayCount2 = 10 * 1024; // 10 KB
            int[] array2 = Enumerable.Repeat(1, arrayCount2).ToArray();

            int result = 0;
            for (int i = 0; i < arrayCount2; i += 16) // 16 because sizeof(int) = 4, 4 * 16 = 64 bytes.
            {
                result += array2[i];
            }

            return result;
        }

        [Benchmark(Description = "Clean Cache doesn't affect performance, Benchmark doesn't make allocation.")]
        public int CleanCacheDoesNotAffectsPerformanceWithoutAllocationInBenchmark()
        {
            const int arrayCount2 = 10 * 1024; // 10 KB

            int result = 0;
            for (int i = 0; i < arrayCount2; i += 16) // 16 because sizeof(int) = 4, 4 * 16 = 64 bytes.
            {
                result += Math.Sign(i);
            }

            return result;
        }
    }
}
