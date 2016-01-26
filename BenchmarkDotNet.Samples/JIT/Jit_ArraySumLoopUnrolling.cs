using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.JIT
{
    // See: http://en.wikipedia.org/wiki/Loop_unrolling
    [Config(typeof(Config))]
    public class Jit_ArraySumLoopUnrolling
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.AllJits);
            }
        }

        private const int NUnroll = 1000, N = 1001;

        private readonly int[] nonStaticField;
        private static int[] staticField;

        public Jit_ArraySumLoopUnrolling()
        {
            nonStaticField = staticField = new int[N];
        }

        [Benchmark]
        public int NonStaticUnroll()
        {
            int sum = 0;
            for (int i = 0; i < NUnroll; i++)
                sum += nonStaticField[i];
            return sum;
        }

        [Benchmark]
        public int StaticUnroll()
        {
            int sum = 0;
            for (int i = 0; i < NUnroll; i++)
                sum += staticField[i];
            return sum;
        }

        [Benchmark]
        public int NonStatic()
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
                sum += nonStaticField[i];
            return sum;
        }

        [Benchmark]
        public int Static()
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
                sum += staticField[i];
            return sum;
        }
    }
}