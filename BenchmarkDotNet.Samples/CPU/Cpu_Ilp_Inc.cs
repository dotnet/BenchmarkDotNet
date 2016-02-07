using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.CPU
{
    // See http://en.wikipedia.org/wiki/Instruction-level_parallelism
    [Config(typeof(Config))]
    public class Cpu_Ilp_Inc
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.LegacyJitX86, Job.LegacyJitX64, Job.RyuJitX64);
            }
        }

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