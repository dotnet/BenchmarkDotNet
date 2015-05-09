using System;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    [Task(platform: BenchmarkPlatform.X86, targetIterationCount: 20)]
    public class Cpu_Ilp_Max
    {
        private int[] x = new int[32];

        [Benchmark]
        public int Max()
        {
            var y = x;
            int max = int.MinValue;
            for (int i = 0; i < y.Length; i++)
                max = Math.Max(max, x[i]);
            return max;
        }

        [Benchmark]
        public int MaxEvenOdd()
        {
            var y = x;
            int maxEven = int.MinValue, maxOdd = int.MinValue;
            for (int i = 0; i < y.Length; i += 2)
            {
                maxEven = Math.Max(maxEven, y[i]);
                maxOdd = Math.Max(maxOdd, y[i + 1]);
            }
            return Math.Max(maxEven, maxOdd);
        }
    }
}