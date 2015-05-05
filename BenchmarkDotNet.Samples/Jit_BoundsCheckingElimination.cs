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
        private const int N = 100001, IterationCount = 10001;
        private readonly int[] a = new int[N];

        [Benchmark]
        public int SumN()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    sum += a[i];
            return sum;
        }

        [Benchmark]
        public int SumLength()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < a.Length; i++)
                    sum += a[i];
            return sum;
        }
    }
}