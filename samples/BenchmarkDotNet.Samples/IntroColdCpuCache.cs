using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [RPlotExporter]
    [EnableCacheClearing(CacheClearingStrategy.Allocations)]

    public class IntroColdCpuCacheByAllocations
    {
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

    [RPlotExporter]
    [EnableCacheClearing(CacheClearingStrategy.Native)]

    public class IntroColdCpuCacheByNative
    {
        private readonly int[] array0 = Enumerable.Range(1, 1024 * 1024).ToArray();

        [Benchmark]
        public int NativeStrategy()
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

    [RPlotExporter]
    [EnableCacheClearing(CacheClearingStrategy.None)]
    public class IntroColdCpuCacheByNone
    {
        private readonly int[] array0 = Enumerable.Range(1, 1024 * 1024).ToArray();

        [Benchmark]
        public int NoneStrategy()
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
