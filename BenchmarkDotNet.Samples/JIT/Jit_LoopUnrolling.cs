using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.JIT
{
    [Config(typeof(Config))]
    public class Jit_LoopUnrolling
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.AllJits);
            }
        }

        [Benchmark]
        public int Sum()
        {
            int sum = 0;
            for (int i = 0; i < 1024; i++)
                sum += i;
            return sum;
        }
    }
}