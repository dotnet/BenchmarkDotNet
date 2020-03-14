using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Validators;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Validators
{
    public class ConfigCompatibilityValidatorTests
    {
        private ITestOutputHelper Output { get; }

        public ConfigCompatibilityValidatorTests(ITestOutputHelper output) => Output = output;

        [Fact]
        public void RunningBenchmarksWithIncompatibleConfigsMustFailWithCriticalError()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty().AddLogger(logger);
            var summary =
                BenchmarkSwitcher
                    .FromTypes(new[] { typeof(BenchmarkClassWithExtraOrderer1), typeof(BenchmarkClassWithExtraOrderer2) })
                    .RunAllJoined(config);
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Contains("You use JoinSummary options, but provided configurations cannot be joined", logger.GetLog());
            Assert.Contains("Orderer", logger.GetLog());
        }

        [Fact]
        public void JoinedBenchmarksMustNotHaveDifferentExtraOrderers()
        {
            var benchmarks = new[]
            {
                BenchmarkConverter.TypeToBenchmarks(typeof(BenchmarkClassWithExtraOrderer1)),
                BenchmarkConverter.TypeToBenchmarks(typeof(BenchmarkClassWithExtraOrderer2))
            };

            var cases = benchmarks.SelectMany(b => b.BenchmarksCases).ToArray();

            var validationErrors =
                ConfigCompatibilityValidator
                    .FailOnError
                    .Validate(new ValidationParameters(cases, null))
                    .ToArray();

            Assert.NotEmpty(validationErrors);
            Assert.StartsWith("You use JoinSummary options, but provided configurations cannot be joined", validationErrors.Single().Message);
            Assert.Contains("Orderer", validationErrors.Single().Message);
        }

        [Fact]
        public void JoinedBenchmarksMayHaveOneExtraOrderer()
        {
            var benchmarks = new[]
            {
                BenchmarkConverter.TypeToBenchmarks(typeof(BenchmarkClassWithExtraOrderer1)),
                BenchmarkConverter.TypeToBenchmarks(typeof(BenchmarkClassWithDefaultOrderer1))
            };

            var cases = benchmarks.SelectMany(b => b.BenchmarksCases).ToArray();

            var validationErrors =
                ConfigCompatibilityValidator
                    .FailOnError
                    .Validate(new ValidationParameters(cases, null))
                    .ToArray();

            Assert.Empty(validationErrors);
        }

        [Fact]
        public void JoinedBenchmarksMayHaveDefaultOrderers()
        {
            var benchmarks = new[]
            {
                BenchmarkConverter.TypeToBenchmarks(typeof(BenchmarkClassWithDefaultOrderer1)),
                BenchmarkConverter.TypeToBenchmarks(typeof(BenchmarkClassWithDefaultOrderer2))
            };

            var cases = benchmarks.SelectMany(b => b.BenchmarksCases).ToArray();

            var validationErrors =
                ConfigCompatibilityValidator
                    .FailOnError
                    .Validate(new ValidationParameters(cases, null))
                    .ToArray();

            Assert.Empty(validationErrors);
        }

        [Orderer(SummaryOrderPolicy.Method)]
        public class BenchmarkClassWithExtraOrderer1
        {
            [Benchmark]
            public void Foo() { }
        }

        [Orderer(SummaryOrderPolicy.Method)]
        public class BenchmarkClassWithExtraOrderer2
        {
            [Benchmark]
            public void Bar() { }
        }

        public class BenchmarkClassWithDefaultOrderer1
        {
            [Benchmark]
            public void Baz() { }
        }

        public class BenchmarkClassWithDefaultOrderer2
        {
            [Benchmark]
            public void Buzz() { }
        }
    }
}
