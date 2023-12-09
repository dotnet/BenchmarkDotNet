using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Order
{
    public class DefaultOrdererTests
    {
        private static Summary CreateMockSummary() => new ("", ImmutableArray<BenchmarkReport>.Empty, HostEnvironmentInfo.GetCurrent(),
            "", "", TimeSpan.Zero, CultureInfo.InvariantCulture, ImmutableArray<ValidationError>.Empty, ImmutableArray<IColumnHidingRule>.Empty);

        private static BenchmarkCase CreateBenchmarkCase(string category, int parameter, params BenchmarkLogicalGroupRule[] rules) => new (
            new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo, categories: new[] { category }),
            new Job(),
            new ParameterInstances(new[]
            {
                new ParameterInstance(new ParameterDefinition("P", false, null, false, null, 0), parameter, SummaryStyle.Default)
            }),
            DefaultConfig.Instance.AddLogicalGroupRules(rules).CreateImmutableConfig()
        );

        private static string GetId(BenchmarkCase benchmarkCase) => benchmarkCase.Descriptor.Categories.First() + benchmarkCase.Parameters.Items.First().Value;

        [Fact]
        public void CategoriesHasHigherPriorityThanParameters()
        {
            var summary = CreateMockSummary();
            var benchmarkCases = new List<BenchmarkCase>
            {
                CreateBenchmarkCase("A", 1),
                CreateBenchmarkCase("B", 1),
                CreateBenchmarkCase("A", 2),
                CreateBenchmarkCase("B", 2)
            }.ToImmutableArray();
            string[] sortedBenchmarkCases = DefaultOrderer.Instance.GetSummaryOrder(benchmarkCases, summary).Select(GetId).ToArray();
            Assert.Equal(new[] { "A1", "A2", "B1", "B2" }, sortedBenchmarkCases);
        }

        [Fact]
        public void OrderCanBeOverriden()
        {
            BenchmarkLogicalGroupRule[] rules =
            {
                BenchmarkLogicalGroupRule.ByParams,
                BenchmarkLogicalGroupRule.ByCategory,
            };
            var summary = CreateMockSummary();
            var benchmarkCases = new List<BenchmarkCase>
            {
                CreateBenchmarkCase("A", 1, rules),
                CreateBenchmarkCase("B", 1, rules),
                CreateBenchmarkCase("A", 2, rules),
                CreateBenchmarkCase("B", 2, rules)
            }.ToImmutableArray();
            string[] sortedBenchmarkCases = DefaultOrderer.Instance.GetSummaryOrder(benchmarkCases, summary).Select(GetId).ToArray();
            Assert.Equal(new[] { "A1", "B1", "A2", "B2" }, sortedBenchmarkCases);
        }
    }
}