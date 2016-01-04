using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Tasks;
using System;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    // See https://github.com/PerfDotNet/BenchmarkDotNet/issues/55
    // https://github.com/PerfDotNet/BenchmarkDotNet/issues/59 is also related
    public class InnerClassTest
    {
        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var plugins = BenchmarkPluginBuilder.CreateDefault().AddLogger(logger).Build();
            var reports = new BenchmarkRunner(plugins).Run<InnerClassTest>();
            var testLog = logger.GetLog();
            Assert.Contains("// ### BenchmarkInnerClass method called ###" + Environment.NewLine, testLog);
            Assert.Contains("// ### BenchmarkGenericInnerClass method called ###" + Environment.NewLine, testLog);
            Assert.DoesNotContain("No benchmarks found", logger.GetLog());
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public Tuple<Outer, Outer.Inner> BenchmarkInnerClass()
        {
            Console.WriteLine("// ### BenchmarkInnerClass method called ###");
            return Tuple.Create(new Outer(), new Outer.Inner());
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public Tuple<Outer, Outer.InnerGeneric<string>> BenchmarkGenericInnerClass()
        {
            Console.WriteLine("// ### BenchmarkGenericInnerClass method called ###");
            return Tuple.Create(new Outer(), new Outer.InnerGeneric<string>());
        }
    }

    public class Outer
    {
        public class Inner
        {
        }

        public class InnerGeneric<T>
        {
        }
    }
}
