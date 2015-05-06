using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    // Bounds-checking elimination
    [Task(platform: BenchmarkPlatform.X86)]
    [Task(platform: BenchmarkPlatform.X64)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Jit_BoundsCheckingElimination
    {
        private const int N = 11, IterationCount = 100000001;
        private readonly int[] x = new int[N];

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumNImplementation(int[] a)
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    sum += a[i];
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumLengthImplementation(int[] a)
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < a.Length; i++)
                    sum += a[i];
            return sum;
        }

        [Benchmark]
        public int SumN()
        {
            return SumNImplementation(x);
        }

        [Benchmark]
        public int SumLength()
        {
            return SumLengthImplementation(x);
        }
    }
}