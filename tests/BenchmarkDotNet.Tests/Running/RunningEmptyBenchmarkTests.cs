using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Parameters;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Tests.Running
{
    public class RunningEmptyBenchmarkTests
    {
        [Fact]
        public void WhenNoBenchmarksAreFound_ReturnsNoBenchmarksHaveBeenFoundValidationError()
        {
            var logger = new AccumulationLogger();
            var config = ManualConfig.CreateEmpty().AddLogger(logger);
            var summaries = BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmarks), config) });
            Console.WriteLine(logger.GetLog());
            Assert.Contains("// No benchmarks have been found", logger.GetLog());
        }

        public class EmptyBenchmarks
        {
            // No benchmark methods here
        }
    }
}