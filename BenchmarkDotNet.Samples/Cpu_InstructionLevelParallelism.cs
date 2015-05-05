using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    [Task(
     mode: BenchmarkMode.SingleRun,
     platform: BenchmarkPlatform.X86)]
    [Task(
     mode: BenchmarkMode.SingleRun,
     platform: BenchmarkPlatform.X64,
     jitVersion: BenchmarkJitVersion.LegacyJit)]
    [Task(
     mode: BenchmarkMode.SingleRun,
     platform: BenchmarkPlatform.X64,
     jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Cpu_InstructionLevelParallelism
    {
        private const int N = 100001, IterationCount = 40001;

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
                for (int i = 0; i < N - 1; i += 2)
                {
                    maxEven = Math.Max(a[i], maxEven);
                    maxOdd = Math.Max(a[i + 1], maxOdd);
                }
            return Math.Max(maxEven, maxOdd);
        }
    }
}