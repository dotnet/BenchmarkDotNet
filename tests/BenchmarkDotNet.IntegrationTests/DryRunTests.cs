using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DryRunTests : BenchmarkTestExecutor
    {
        public DryRunTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void WelchTTest() => CanExecute<WelchTTestBench>();

        [DryJob, StatisticalTestColumn]
        public class WelchTTestBench
        {
            [Benchmark(Baseline = true)]
            public void A() => Thread.Sleep(10);

            [Benchmark]
            public void B() => Thread.Sleep(10);
        }

        [Fact]
        public void ColdStart()
        {
            var summary = CanExecute<ColdStartBench>();

            var report = summary.Reports.Single();

            Assert.Equal(2, report.AllMeasurements.Count);

            foreach (var measurement in report.AllMeasurements)
            {
                Assert.Equal(1, measurement.LaunchIndex);
                Assert.Equal(1, measurement.IterationIndex);
                Assert.Equal(IterationMode.Workload, measurement.IterationMode);
                Assert.True(measurement.IterationStage is IterationStage.Actual or IterationStage.Result);
            }
        }

        [DryJob]
        public class ColdStartBench
        {
            private int counter;

            [Benchmark]
            public void Foo()
            {
                if (++counter > 1)
                {
                    throw new InvalidOperationException("Benchmark was executed more than once");
                }
            }
        }
    }
}