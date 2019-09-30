using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Text;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    // *** Attribute Style ***

    [EncodingAttribute.Unicode]
    public class IntroEncoding
    {
        [Benchmark]
        public long Foo()
        {
            long waitUntil = Stopwatch.GetTimestamp() + 1000;
            while (Stopwatch.GetTimestamp() < waitUntil) { }

            return waitUntil;
        }
    }

    // *** Object Style ***

    [Config(typeof(Config))]
    public class IntroEncodingObjectStyle
    {
        private class Config : ManualConfig
        {
            public Config() => Encoding = Encoding.Unicode;
        }

        [Benchmark]
        public long Foo()
        {
            long waitUntil = Stopwatch.GetTimestamp() + 1000;
            while (Stopwatch.GetTimestamp() < waitUntil) { }

            return waitUntil;
        }
    }

    // *** Fluent Config ***

    public class IntroEncodingFluentConfig
    {
        public static void Run()
        {
            BenchmarkRunner.Run<IntroEncodingFluentConfig>(
                ManualConfig
                    .Create(DefaultConfig.Instance)
                    .With(Encoding.Unicode));
        }

        [Benchmark]
        public long Foo()
        {
            long waitUntil = Stopwatch.GetTimestamp() + 1000;
            while (Stopwatch.GetTimestamp() < waitUntil) { }

            return waitUntil;
        }
    }
}