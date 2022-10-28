using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ExceptionDiagnoserTests
    {
        [Fact]
        public void ExceptionCountIsAccurate()
        {
            var config = CreateConfig();

            var summary = BenchmarkRunner.Run<ExceptionCount>(config);

            AssertStats(summary, new Dictionary<string, (string metricName, double expectedValue)>
            {
                { nameof(ExceptionCount.DoNothing), ("ExceptionFrequency", 0.0) },
                { nameof(ExceptionCount.ThrowOneException), ("ExceptionFrequency", 1.0) }
            });
        }

        public class ExceptionCount
        {
            [Benchmark]
            public void ThrowOneException()
            {
                try
                {
                    throw new Exception();
                }
                catch { }
            }

            [Benchmark]
            public void DoNothing() { }
        }

        private IConfig CreateConfig()
            => ManualConfig.CreateEmpty()
                .AddJob(Job.ShortRun
                    .WithEvaluateOverhead(false) // no need to run idle for this test
                    .WithWarmupCount(0) // don't run warmup to save some time for our CI runs
                    .WithIterationCount(1)) // single iteration is enough for us
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddDiagnoser(ExceptionDiagnoser.Default);

        private void AssertStats(Summary summary, Dictionary<string, (string metricName, double expectedValue)> assertions)
        {
            foreach (var assertion in assertions)
            {
                var selectedReport = summary.Reports.Single(report => report.BenchmarkCase.DisplayInfo.Contains(assertion.Key));
                var metric = selectedReport.Metrics.Single(m => m.Key == assertion.Value.metricName);
                Assert.Equal(assertion.Value.expectedValue, metric.Value.Value);
            }
        }
    }
}
