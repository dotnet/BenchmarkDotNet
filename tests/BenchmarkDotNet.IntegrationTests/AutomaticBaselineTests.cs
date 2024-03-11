using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Mocks;
using System;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class AutomaticBaselineTests
    {
        [Fact]
        public void AutomaticBaselineSelectionIsCorrect()
        {
            var config = ManualConfig.CreateEmpty()
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .WithAutomaticBaseline(AutomaticBaselineMode.Fastest);

            var summary = MockFactory.CreateSummary(config, hugeSd: true, Array.Empty<Metric>());
            var table = summary.GetTable(SummaryStyle.Default);
            var method = table.Columns.Single(c => c.Header == "Method");
            var ratio = table.Columns.Single(c => c.Header == "Ratio");

            Assert.Equal(2, method.Content.Length);
            Assert.Equal(nameof(MockFactory.MockBenchmarkClass.Foo), method.Content[0]);
            Assert.Equal(1.0, double.Parse(ratio.Content[0])); // A faster one, see measurements in MockFactory.cs
            Assert.Equal(nameof(MockFactory.MockBenchmarkClass.Bar), method.Content[1]);
            Assert.Equal(1.5, double.Parse(ratio.Content[1]));
        }
    }
}
