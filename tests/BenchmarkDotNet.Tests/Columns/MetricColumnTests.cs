using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Validators;
using Perfolizer.Horology;
using Xunit;

namespace BenchmarkDotNet.Tests.Columns
{
    public class MetricColumnTests
    {
        [Theory]
        [InlineData(false, false, 42_000.0, "42000")]
        [InlineData(true, false, 42_000.0, "42.0000 μs")]
        [InlineData(false, true, 42_000.0, "42.0000")]
        [InlineData(true, true, 42_000.0, "42.0000 μs")]
        public void GetValueTest(bool printUnitsInContent, bool printUnitsInHeader, double metricValue, string expected)
        {
            var column = new MetricColumn(LocalMetricDescriptor.TimeInstance);
            var summary = CreateMockSummary(printUnitsInContent, printUnitsInHeader, TimeUnit.Microsecond, metricValue);
            string actual = column.GetValue(summary, summary.BenchmarksCases.First(), summary.Style);
            Assert.Equal(expected, actual);
        }

        private static Summary CreateMockSummary(bool printUnitsInContent, bool printUnitsInHeader, TimeUnit timeUnit, double metricValue)
        {
            var summaryStyle = new SummaryStyle(TestCultureInfo.Instance, printUnitsInHeader, null, timeUnit, printUnitsInContent);
            var config = new ManualConfig().WithSummaryStyle(summaryStyle);
            var benchmarkCase = new BenchmarkCase(
                new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                Job.Dry,
                new ParameterInstances(ImmutableArray<ParameterInstance>.Empty),
                ImmutableConfigBuilder.Create(config));
            var metric = new Metric(LocalMetricDescriptor.TimeInstance, metricValue);
            var benchmarkReport = new BenchmarkReport(true, benchmarkCase, null, null, null, new List<Metric>()
            {
                metric
            });
            return new Summary("", new[] { benchmarkReport }.ToImmutableArray(), HostEnvironmentInfo.GetCurrent(),
                "", "", TimeSpan.Zero, CultureInfo.InvariantCulture, ImmutableArray<ValidationError>.Empty, ImmutableArray<IColumnHidingRule>.Empty);
        }

        [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
        private sealed class LocalMetricDescriptor : IMetricDescriptor
        {
            public static readonly IMetricDescriptor TimeInstance = new LocalMetricDescriptor(UnitType.Time);

            private LocalMetricDescriptor(UnitType unitType)
            {
                UnitType = unitType;
            }

            public string Id { get; } = nameof(LocalMetricDescriptor);
            public string DisplayName => Id;
            public string Legend { get; }
            public string NumberFormat { get; }
            public UnitType UnitType { get; }
            public string Unit { get; }
            public bool TheGreaterTheBetter { get; }
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => true;
        }
    }
}