using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    // See http://en.wikipedia.org/wiki/Instruction-level_parallelism
    [Task(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Cpu_Ilp_Inc
    {
        private double a, b, c, d;

        [Benchmark]
        [OperationsPerInvoke(4)]
        public void Parallel()
        {
            a++;
            b++;
            c++;
            d++;
        }

        [Benchmark]
        [OperationsPerInvoke(4)]
        public void Sequential()
        {
            a++;
            a++;
            a++;
            a++;
        }
    }
}