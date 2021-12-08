using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Order
{
    public class DefaultOrdererTests
    {
        [Fact]
        public void CategoriesHasHigherPriorityThanParameters()
        {
            var summary = new Summary("", ImmutableArray<BenchmarkReport>.Empty, HostEnvironmentInfo.GetCurrent(),
                "", "", TimeSpan.Zero, CultureInfo.InvariantCulture, ImmutableArray<ValidationError>.Empty);

            BenchmarkCase CreateBenchmarkCase(string category, int parameter) => new(
                new Descriptor(null, null, categories: new[] { category }),
                new Job(),
                new ParameterInstances(new[]
                {
                    new ParameterInstance(new ParameterDefinition("P", false, null, false, null, 0), parameter, SummaryStyle.Default)
                }),
                DefaultConfig.Instance.CreateImmutableConfig()
            );

            string GetId(BenchmarkCase benchmarkCase) => benchmarkCase.Descriptor.Categories.First() + benchmarkCase.Parameters.Items.First().Value;

            var benchmarkCases = new List<BenchmarkCase>
            {
                CreateBenchmarkCase("A", 1),
                CreateBenchmarkCase("B", 1),
                CreateBenchmarkCase("A", 2),
                CreateBenchmarkCase("B", 2)
            }.ToImmutableArray();

            string[] sortedBenchmarkCases = DefaultOrderer.Instance.GetSummaryOrder(benchmarkCases, summary).Select(GetId).ToArray();
            Assert.Equal(new[] {"A1", "A2", "B1", "B2"}, sortedBenchmarkCases);
        }
    }
}