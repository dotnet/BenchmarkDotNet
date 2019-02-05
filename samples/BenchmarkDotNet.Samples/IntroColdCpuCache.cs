using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics.StatisticalTesting;

namespace BenchmarkDotNet.Samples
{
   // [StatisticalTestColumn(StatisticalTestKind.MannWhitney, ThresholdUnit.Microseconds, 0.2, true)]
    [Config(typeof(CompareCacheClearingStrategies))]
    public class IntroColdCpuCache
    {
        private class CompareCacheClearingStrategies : ManualConfig
        {
            public CompareCacheClearingStrategies()
            {
                Add(Job.ShortRun.WithCacheClearingStrategy(CacheClearingStrategy.None).AsBaseline());
                Add(Job.ShortRun.WithCacheClearingStrategy(CacheClearingStrategy.Allocations));

                Add(Job.ShortRun.WithAffinity((IntPtr) 1).WithCacheClearingStrategy(CacheClearingStrategy.None));
                Add(Job.ShortRun.WithAffinity((IntPtr) 1).WithCacheClearingStrategy(CacheClearingStrategy.Allocations));
                
                Add(HardwareCounter.CacheMisses);
                Add(MemoryDiagnoser.Default);
            }
        }

        private const int arrayCount = 14 * 1024 * 1024; 
        private readonly int[] array = Enumerable.Repeat(1, arrayCount).ToArray(); 

        [Benchmark]
        public int ArraySum()
        {
            int result = 0;
            for (int i = 0; i < arrayCount; i += 16) // 16 because sizeof(int) = 4, 4 * 16 = 64 bytes.
            {
                result += array[i];
            }

            return result;
        }

        [Benchmark]
        public void Accessing()
        {
            for (int i = 0; i < arrayCount; i += 16) // 16 because sizeof(int) = 4, 4 * 16 = 64 bytes.
            {
                array[i] = 1;
            }
        }

        private readonly int[,] tweDimensionalArray = new int[5000, 5000];

        [Benchmark]
        public void ArrayOfArrayAccessingByRows()
        {
            for (int i = 0; i < 5000; i++)
            {
                for (int j = 0; j < 5000; j++)
                {
                    tweDimensionalArray[i, j] = 1;
                }
            }
        }

        [Benchmark]
        public void ArrayOfArrayAccessingByColumns()
        {
            for (int i = 0; i < 5000; i++)
            {
                for (int j = 0; j < 5000; j++)
                {
                    tweDimensionalArray[j, i] = 1;
                }
            }
        }
    }
}
