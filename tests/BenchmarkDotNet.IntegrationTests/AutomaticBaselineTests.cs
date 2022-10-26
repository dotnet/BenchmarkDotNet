using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Linq;
using System.Threading;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class AutomaticBaselineTests
    {
        [Fact]
        public void AutomaticBaselineSelectionIsCorrect()
        {
            var config = CreateConfig();

            var summary = BenchmarkRunner.Run<BaselineSample>();

            var table = summary.GetTable(SummaryStyle.Default);
            var column = table.Columns.Single(c => c.Header == "Ratio");
            Assert.Equal(2, column.Content.Length);
            Assert.Equal(1.0, double.Parse(column.Content[1])); // Ratio of TwoMilliseconds
            Assert.True(double.Parse(column.Content[0]) > 1.0); // Ratio of TwoHundredMilliseconds
        }

        [AutomaticBaseline(AutomaticBaselineMode.Fastest)]
        public class BaselineSample
        {
            [Benchmark]
            public void TwoHundredMilliseconds()
            {
                Thread.Sleep(200);
            }

            [Benchmark]
            public void TwoMilliseconds()
            {
                Thread.Sleep(2);
            }
        }

        private IConfig CreateConfig()
            => ManualConfig.CreateEmpty()
                .AddJob(Job.ShortRun
                    .WithEvaluateOverhead(false) // no need to run idle for this test
                    .WithWarmupCount(0) // don't run warmup to save some time for our CI runs
                    .WithIterationCount(1)) // single iteration is enough for us
                .AddColumnProvider(DefaultColumnProviders.Instance);
    }
}
