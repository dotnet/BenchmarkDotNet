using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.CPU
{
    // See http://en.wikipedia.org/wiki/Instruction-level_parallelism
    [LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
    public class Cpu_Ilp_VsBce
    {
        private int[] a = new int[4];

        [Benchmark(OperationsPerInvoke = 4)]
        public void Parallel()
        {
            a[0]++;
            a[1]++;
            a[2]++;
            a[3]++;
        }

        [Benchmark(OperationsPerInvoke = 4)]
        public void Sequential()
        {
            a[0]++;
            a[0]++;
            a[0]++;
            a[0]++;
        }
    }
}