using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    // See http://en.wikipedia.org/wiki/Bounds-checking_elimination
    [Task(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Jit_Bce
    {
        private const int N = 101;
        private int[] x = new int[N];

        [Benchmark]
        public int SumN()
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
                sum += x[i];
            return sum;
        }

        [Benchmark]
        public int SumLength()
        {
            int sum = 0;
            for (int i = 0; i < x.Length; i++)
                sum += x[i];
            return sum;
        }
    }
}