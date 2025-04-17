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

namespace BenchmarkDotNet.Tests.Running
{
    public class RunningEmptyBenchmarkTests
    {
        [Fact]
        public void WhenNoBenchmarksAreFound_ReturnsNoBenchmarksHaveBeenFoundValidationError()
        {
            var summaries = BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmarks), null) });
            // Act
            // Assert
            Assert.Single(summaries);
            var summary = summaries[0];
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Equal("No benchmarks have been found", summary.ValidationErrors[0].Message);
            Assert.True(summary.ValidationErrors[0].IsCritical);
        }

        public class EmptyBenchmarks
        {
            // No benchmark methods here
        }
    }
}