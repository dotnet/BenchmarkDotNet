using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    // See https://github.com/dotnet/BenchmarkDotNet/issues/55
    // https://github.com/dotnet/BenchmarkDotNet/issues/59 is also related
    public class InnerClassTest : BenchmarkTestExecutor
    {
        private const string FirstExpectedMessage = "// ### BenchmarkInnerClass method called ###";
        private const string SecondExpectedMessage = "// ### BenchmarkGenericInnerClass method called ###";

        public InnerClassTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void InnerClassesAreSupported()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            var summary = CanExecute<Inner>(config);

            string[] expected = new string[]
            {
                FirstExpectedMessage,
                SecondExpectedMessage
            };

            Assert.Equal(expected, GetCombinedStandardOutput(summary));
        }

        public class Inner
        {
            [Benchmark]
            public Tuple<Outer, Outer.Inner> BenchmarkInnerClass()
            {
                Console.WriteLine(FirstExpectedMessage);
                return Tuple.Create(new Outer(), new Outer.Inner());
            }

            [Benchmark]
            public Tuple<Outer, Outer.InnerGeneric<string>> BenchmarkGenericInnerClass()
            {
                Console.WriteLine(SecondExpectedMessage);
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
