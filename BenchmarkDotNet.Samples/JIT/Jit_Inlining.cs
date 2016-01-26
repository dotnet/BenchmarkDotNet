using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.JIT
{
    // See http://en.wikipedia.org/wiki/Inline_expansion
    // See http://aakinshin.net/en/blog/dotnet/inlining-and-starg/
    [Config(typeof(Config))]
    public class Jit_Inlining
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.AllJits);
            }
        }

        [Benchmark]
        public int Calc()
        {
            return WithoutStarg(0x11) + WithStarg(0x12);
        }

        private static int WithoutStarg(int value)
        {
            return value;
        }

        private static int WithStarg(int value)
        {
            if (value < 0)
                value = -value;
            return value;
        }
    }
}