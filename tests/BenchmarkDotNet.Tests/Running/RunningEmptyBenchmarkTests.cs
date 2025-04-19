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
using System.Linq;

namespace BenchmarkDotNet.Tests.Running
{
    public class RunningEmptyBenchmarkTests
    {
        [Fact]
        public void WhenRunningSingleEmptyBenchmark_ValidationErrorIsThrown()
        {
            var summaries = BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark), null) });
            Assert.Single(summaries);
            var summary = summaries[0];
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Single(summary.ValidationErrors);
            Assert.Equal($"No [Benchmark] attribute found on '{typeof(EmptyBenchmark).Name}' benchmark case.", summary.ValidationErrors[0].Message);
        }

        [Fact]
        public void WhenRunningMultipleEmptyBenchmarks_ValidationErrorIsThrown()
        {
            var summaries = BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark), null), BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark), null) });
            Assert.Single(summaries);
            var summary = summaries[0];
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Equal(2, summary.ValidationErrors.Count());
            Assert.Equal($"No [Benchmark] attribute found on '{typeof(EmptyBenchmark).Name}' benchmark case.", summary.ValidationErrors[0].Message);
            Assert.Equal($"No [Benchmark] attribute found on '{typeof(EmptyBenchmark).Name}' benchmark case.", summary.ValidationErrors[1].Message);
        }

        [Fact]
        public void WhenRunningMultipleBenchmarksOneOfWhichIsEmpty_ValidationErrorIsThrown()
        {
            var summaries = BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark), null), BenchmarkConverter.TypeToBenchmarks(typeof(NotEmptyBenchmark), null) });
            Assert.Single(summaries);
            var summary = summaries[0];
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == $"No [Benchmark] attribute found on '{typeof(EmptyBenchmark).Name}' benchmark case.");
        }

        public class EmptyBenchmark
        {
        }

        public class NotEmptyBenchmark
        {
            [Benchmark]
            public void Benchmark()
            {
                var sum = 0;
                for (int i = 0; i < 1; i++)
                {
                    sum += i;
                }
            }
        }
    }
}