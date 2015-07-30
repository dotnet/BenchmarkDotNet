using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    // See http://en.wikipedia.org/wiki/Instruction-level_parallelism
    [BenchmarkTask(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Cpu_Ilp_VsBce
    {
        private int[] a = new int[4];

        [Benchmark]
        [OperationsPerInvoke(4)]
        public void Parallel()
        {
            a[0]++;
            a[1]++;
            a[2]++;
            a[3]++;
        }

        [Benchmark]
        [OperationsPerInvoke(4)]
        public void Sequential()
        {
            a[0]++;
            a[0]++;
            a[0]++;
            a[0]++;
        }
    }
}