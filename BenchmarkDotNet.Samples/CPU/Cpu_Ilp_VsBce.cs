using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.CPU
{
    // See http://en.wikipedia.org/wiki/Instruction-level_parallelism
    [Config(typeof(Config))]
    public class Cpu_Ilp_VsBce
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.LegacyX86, Job.LegacyX64, Job.RyuJitX64);
            }
        }

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