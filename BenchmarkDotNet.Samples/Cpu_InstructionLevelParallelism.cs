using System;
using System.Runtime.CompilerServices;
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
        private const int N = 10000, IterationCount = 40001;

        private readonly int[] a = new int[N];

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int MaxImplementation(int n)
        {
            var max = int.MinValue;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < n; i++)
                    max = Math.Max(a[i], max);
            return max;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int MaxEvenOddImplementation(int n)
        {
            int maxEven = int.MinValue, maxOdd = int.MinValue;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < n; i += 2)
                {
                    maxEven = Math.Max(a[i], maxEven);
                    maxOdd = Math.Max(a[i + 1], maxOdd);
                }
            return Math.Max(maxEven, maxOdd);
        }

        [Benchmark]
        public int Max()
        {
            return MaxImplementation(N);
        }

        [Benchmark]
        public int MaxEvenOdd()
        {
            return MaxEvenOddImplementation(N);
        }
    }
}