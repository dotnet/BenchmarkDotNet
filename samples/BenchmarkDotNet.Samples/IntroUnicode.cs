using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    // *** Attribute Style ***
    [UnicodeConsoleLogger]
    public class IntroUnicode
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
    public class IntroUnicodeObjectStyle
    {
        private class Config : ManualConfig
        {
            public Config() => AddLogger(ConsoleLogger.Unicode);
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
    public class IntroUnicodeFluentConfig
    {
        public static void Run()
        {
            BenchmarkRunner.Run<IntroUnicodeFluentConfig>(
                DefaultConfig.Instance
                    .AddLogger(ConsoleLogger.Unicode));
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
