using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Samples.JIT
{
    // See: http://en.wikipedia.org/wiki/Loop_unrolling
    [Config(typeof(Config))]
    [LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
    public class Jit_ArraySumLoopUnrolling
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(new TagColumn("N", name => (name.Contains("Unroll") ? NUnroll : N).ToString()));
                Add(new TagColumn("Static", name => name.Contains("NonStatic") ? "No" : "Yes"));
                Add(RankColumn.Arabic);
            }
        }

        private const int NUnroll = 10000, N = 10001;

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