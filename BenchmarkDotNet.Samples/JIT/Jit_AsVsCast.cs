using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.JIT
{
    [Config(typeof(Config))]
    public class Jit_AsVsCast
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.AllJits);
            }
        }

        public class Foo
        {
        }

        private object foo = new Foo();

        [Benchmark]
        public Foo As()
        {
            return foo as Foo;
        }

        [Benchmark]
        public object Cast()
        {
            return (Foo)foo;
        }
    }
}