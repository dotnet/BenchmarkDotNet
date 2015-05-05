using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    public class Trivial_SimpleSum
    {
        [Benchmark]
        [Task(
            mode: BenchmarkMode.SingleRun,
            platform: BenchmarkPlatform.AnyCpu,
            jitVersion: BenchmarkJitVersion.LegacyJit,
            warmupIterationCount: 3,
            targetIterationCount: 5)]
        public int SimpleSum()
        {
            int sum = 0;
            for (int i = 0; i < 1000000001; i++)
                sum += i;
            return sum;
        }
    }
}