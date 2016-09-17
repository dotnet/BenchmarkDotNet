using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.CPU
{
    // See http://en.wikipedia.org/wiki/Instruction-level_parallelism
    [LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
    public class Cpu_Ilp_Inc
    {
        private double a, b, c, d;

        [Benchmark(OperationsPerInvoke = 4)]
        public void Parallel()
        {
            a++;
            b++;
            c++;
            d++;
        }

        [Benchmark(OperationsPerInvoke = 4)]
        public void Sequential()
        {
            a++;
            a++;
            a++;
            a++;
        }
    }
}