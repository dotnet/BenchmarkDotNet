using BenchmarkDotNet.Jobs;
using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    // See https://github.com/dotnet/BenchmarkDotNet/issues/55
    // https://github.com/dotnet/BenchmarkDotNet/issues/59 is also related
    public class InnerClassTest : BenchmarkTestExecutor
    {
        public InnerClassTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void InnerClassesAreSupported()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<Inner>(config);

            var testLog = logger.GetLog();
            Assert.Contains("// ### BenchmarkInnerClass method called ###" + Environment.NewLine, testLog);
            Assert.Contains("// ### BenchmarkGenericInnerClass method called ###" + Environment.NewLine, testLog);
            Assert.DoesNotContain("No benchmarks found", logger.GetLog());
        }

        public class Inner
        {
            [Benchmark]
            public Tuple<Outer, Outer.Inner> BenchmarkInnerClass()
            {
                Console.WriteLine("// ### BenchmarkInnerClass method called ###");
                return Tuple.Create(new Outer(), new Outer.Inner());
            }

            [Benchmark]
            public Tuple<Outer, Outer.InnerGeneric<string>> BenchmarkGenericInnerClass()
            {
                Console.WriteLine("// ### BenchmarkGenericInnerClass method called ###");
                return Tuple.Create(new Outer(), new Outer.InnerGeneric<string>());
            }
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
