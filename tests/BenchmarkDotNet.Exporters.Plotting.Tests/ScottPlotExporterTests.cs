using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Tests.Mocks;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Exporters.Plotting.Tests
{
    public class ScottPlotExporterTests(ITestOutputHelper output)
    {
        public static TheoryData<Type> GetGroupBenchmarkTypes()
        {
            var data = new TheoryData<Type>();
            foreach (var type in typeof(BaselinesBenchmarks).GetNestedTypes())
                data.Add(type);
            return data;
        }

        [Theory]
        [MemberData(nameof(GetGroupBenchmarkTypes))]
        public void BarPlots(Type benchmarkType)
        {
            var logger = new AccumulationLogger();
            logger.WriteLine("=== " + benchmarkType.Name + " ===");

            var exporter = new ScottPlotExporter()
            {
                IncludeBarPlot = true,
                IncludeBoxPlot = false,
            };
            var summary = MockFactory.CreateSummary(benchmarkType);
            var filePaths = exporter.ExportToFiles(summary, logger).ToList();
            Assert.NotEmpty(filePaths);
            Assert.All(filePaths, f => File.Exists(f));

            foreach (string filePath in filePaths)
                logger.WriteLine($"* {filePath}");
            output.WriteLine(logger.GetLog());
        }

        [Theory]
        [MemberData(nameof(GetGroupBenchmarkTypes))]
        public void BoxPlots(Type benchmarkType)
        {
            var logger = new AccumulationLogger();
            logger.WriteLine("=== " + benchmarkType.Name + " ===");

            var exporter = new ScottPlotExporter()
            {
                IncludeBarPlot = false,
                IncludeBoxPlot = true,
            };
            var summary = MockFactory.CreateSummaryWithBiasedDistribution(benchmarkType, 1, 4, 10, 9);
            var filePaths = exporter.ExportToFiles(summary, logger).ToList();
            Assert.NotEmpty(filePaths);
            Assert.All(filePaths, f => File.Exists(f));

            foreach (string filePath in filePaths)
                logger.WriteLine($"* {filePath}");
            output.WriteLine(logger.GetLog());
        }

        [Theory]
        [MemberData(nameof(GetGroupBenchmarkTypes))]
        public void BoxPlotsWithOneMeasurement(Type benchmarkType)
        {
            var logger = new AccumulationLogger();
            logger.WriteLine("=== " + benchmarkType.Name + " ===");

            var exporter = new ScottPlotExporter()
            {
                IncludeBarPlot = false,
                IncludeBoxPlot = true,
            };
            var summary = MockFactory.CreateSummaryWithBiasedDistribution(benchmarkType, 1, 4, 10, 1);
            var filePaths = exporter.ExportToFiles(summary, logger).ToList();
            Assert.NotEmpty(filePaths);
            Assert.All(filePaths, f => File.Exists(f));

            foreach (string filePath in filePaths)
                logger.WriteLine($"* {filePath}");
            output.WriteLine(logger.GetLog());
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static class BaselinesBenchmarks
        {
            /* NoBaseline */
            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            public class NoBaseline_MethodsParamsJobs
            {
                [Params(2, 10)] public int Param;

                [Benchmark] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
            public class NoBaseline_MethodsParamsJobs_GroupByMethod
            {
                [Params(2, 10)] public int Param;

                [Benchmark, BenchmarkCategory("CatA")] public void Base() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Foo() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByJob)]
            public class NoBaseline_MethodsParamsJobs_GroupByJob
            {
                [Params(2, 10)] public int Param;

                [Benchmark, BenchmarkCategory("CatA")] public void Base() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Foo() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
            public class NoBaseline_MethodsParamsJobs_GroupByParams
            {
                [Params(2, 10)] public int Param;

                [Benchmark, BenchmarkCategory("CatA")] public void Base() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Foo() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
            public class NoBaseline_MethodsParamsJobs_GroupByCategory
            {
                [Params(2, 10)] public int Param;

                [Benchmark(Baseline = true), BenchmarkCategory("CatA")]
                public void A1() { }

                [Benchmark, BenchmarkCategory("CatA")] public void A2() { }

                [Benchmark(Baseline = true), BenchmarkCategory("CatB")]
                public void B1() { }

                [Benchmark, BenchmarkCategory("CatB")] public void B2() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(
                BenchmarkLogicalGroupRule.ByMethod,
                BenchmarkLogicalGroupRule.ByJob,
                BenchmarkLogicalGroupRule.ByParams,
                BenchmarkLogicalGroupRule.ByCategory)]
            public class NoBaseline_MethodsParamsJobs_GroupByAll
            {
                [Params(2, 10)] public int Param;

                [Benchmark(Baseline = true), BenchmarkCategory("CatA")]
                public void A1() { }

                [Benchmark, BenchmarkCategory("CatA")] public void A2() { }

                [Benchmark(Baseline = true), BenchmarkCategory("CatB")]
                public void B1() { }

                [Benchmark, BenchmarkCategory("CatB")] public void B2() { }
            }

            /* MethodBaseline */

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            public class MethodBaseline_Methods
            {
                [Benchmark(Baseline = true)] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            public class MethodBaseline_MethodsParams
            {
                [Params(2, 10)] public int Param;

                [Benchmark(Baseline = true)] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            public class MethodBaseline_MethodsJobs
            {
                [Benchmark(Baseline = true)] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            public class MethodBaseline_MethodsParamsJobs
            {
                [Params(2, 10)] public int Param;

                [Benchmark(Baseline = true)] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            /* JobBaseline */

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1", baseline: true), SimpleJob(id: "Job2")]
            public class JobBaseline_MethodsJobs
            {
                [Benchmark] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1", baseline: true), SimpleJob(id: "Job2")]
            public class JobBaseline_MethodsParamsJobs
            {
                [Params(2, 10)] public int Param;

                [Benchmark] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            /* MethodJobBaseline */

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1", baseline: true), SimpleJob(id: "Job2")]
            public class MethodJobBaseline_MethodsJobs
            {
                [Benchmark(Baseline = true)] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1", baseline: true), SimpleJob(id: "Job2")]
            public class MethodJobBaseline_MethodsJobsParams
            {
                [Params(2, 10)] public int Param;

                [Benchmark(Baseline = true)] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            /* Invalid */

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            public class Invalid_TwoMethodBaselines
            {
                [Benchmark(Baseline = true)] public void Foo() { }
                [Benchmark(Baseline = true)] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, BaselineColumn]
            [SimpleJob(id: "Job1", baseline: true), SimpleJob(id: "Job2", baseline: true)]
            public class Invalid_TwoJobBaselines
            {
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            /* Escape Params */

            public class Escape_ParamsAndArguments
            {
                [Params("\t", "\n")] public string StringParam;

                [Arguments('\t')]
                [Arguments('\n')]
                [Benchmark] public void Foo(char charArg) { }

                [Benchmark] public void Bar() { }
            }
        }
    }
}