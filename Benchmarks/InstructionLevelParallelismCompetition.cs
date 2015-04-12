using System;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class InstructionLevelParallelismCompetition
    {
        private const int N = 100000, IterationCount = 40000;

        private readonly int[] a = new int[N];

        [Benchmark]
        public int Max()
        {
            var max = int.MinValue;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    max = Math.Max(a[i], max);
            return max;
        }

        [Benchmark]
        public int MaxEvenOdd()
        {
            int maxEven = int.MinValue, maxOdd = int.MinValue;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i += 2)
                {
                    maxEven = Math.Max(a[i], maxEven);
                    maxOdd = Math.Max(a[i + 1], maxOdd);
                }
            return Math.Max(maxEven, maxOdd);
        }
    }
}