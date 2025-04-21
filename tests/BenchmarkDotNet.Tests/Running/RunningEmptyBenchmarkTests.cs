using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;
using System.Linq;

namespace BenchmarkDotNet.Tests.Running
{
    public class RunningEmptyBenchmarkTests
    {
        [Fact]
        public void WhenRunningSingleEmptyBenchmark_NoBenchmarkValidationErrorIsThrown()
        {
            var summaries = BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark), null) });
            Assert.Single(summaries);
            var summary = summaries[0];
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == $"No [Benchmark] attribute found on '{typeof(EmptyBenchmark).Name}' benchmark case.");
        }

        [Fact]
        public void WhenRunningMultipleEmptyBenchmarks_NoBenchmarkValidationErrorIsThrown()
        {
            var summaries = BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark), null), BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark2), null) });
            Assert.Single(summaries);
            var summary = summaries[0];
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == $"No [Benchmark] attribute found on '{typeof(EmptyBenchmark).Name}' benchmark case.");
            Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == $"No [Benchmark] attribute found on '{typeof(EmptyBenchmark2).Name}' benchmark case.");
        }

        [Fact]
        public void WhenRunningMultipleBenchmarksOneOfWhichIsEmpty_NoBenchmarkValidationErrorIsThrown()
        {
            var summaries = BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(EmptyBenchmark), null), BenchmarkConverter.TypeToBenchmarks(typeof(NotEmptyBenchmark), null) });
            Assert.Single(summaries);
            var summary = summaries[0];
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == $"No [Benchmark] attribute found on '{typeof(EmptyBenchmark).Name}' benchmark case.");
        }

        [Fact]
        public void WhenRunningEmptyArrayOfBenchmarks_NoBenchmarkValidationErrorIsThrown()
        {
            var summaries = BenchmarkRunnerClean.Run(Array.Empty<BenchmarkRunInfo>());
            Assert.Single(summaries);
            var summary = summaries[0];
            Assert.True(summary.HasCriticalValidationErrors);
            Assert.Contains(summary.ValidationErrors, validationError => validationError.Message == "No benchmarks were found.");
        }

        public class EmptyBenchmark
        {
        }

        public class EmptyBenchmark2
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