using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Perfolizer.Horology;
using Perfolizer.Metrology;
using static BenchmarkDotNet.Reports.SummaryTable.SummaryTableColumn;
using Measurement = BenchmarkDotNet.Reports.Measurement;
using MethodInfo = System.Reflection.MethodInfo;

namespace BenchmarkDotNet.Tests.Mocks
{
    public static class MockFactory
    {
        public static Summary CreateSummary(Type benchmarkType, params IColumnHidingRule[] columHidingRules)
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(benchmarkType);
            return new Summary(
                "MockSummary",
                runInfo.BenchmarksCases.Select((benchmark, index) => CreateReport(benchmark, 30, (index + 1) * 100)).ToImmutableArray(),
                new HostEnvironmentInfoBuilder().WithoutDotNetSdkVersion().Build(),
                string.Empty,
                string.Empty,
                TimeSpan.FromMinutes(1),
                TestCultureInfo.Instance,
                ImmutableArray<ValidationError>.Empty,
                ImmutableArray.Create<IColumnHidingRule>(columHidingRules));
        }

        public static Summary CreateSummaryWithBiasedDistribution(Type benchmarkType, int min, int median, int max, int n)
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(benchmarkType);
            return new Summary(
                $"MockSummary-N{n}",
                runInfo.BenchmarksCases.Select((benchmark, index) => CreateReportWithBiasedDistribution(
                    benchmark,
                    (index + 1) * min,
                    (index + 1) * median,
                    (index + 1) * max,
                    n,
                    Array.Empty<Metric>())).ToImmutableArray(),
                new HostEnvironmentInfoBuilder().WithoutDotNetSdkVersion().Build(),
                string.Empty,
                string.Empty,
                TimeSpan.FromMinutes(1),
                TestCultureInfo.Instance,
                ImmutableArray<ValidationError>.Empty,
                ImmutableArray<IColumnHidingRule>.Empty);
        }

        public static Summary CreateSummary(IConfig config) => new Summary(
            "MockSummary",
            CreateReports(config),
            new HostEnvironmentInfoBuilder().WithoutDotNetSdkVersion().Build(),
            string.Empty,
            string.Empty,
            TimeSpan.FromMinutes(1),
            config.CultureInfo,
            ImmutableArray<ValidationError>.Empty,
            ImmutableArray<IColumnHidingRule>.Empty,
            config.SummaryStyle);


        public static Summary CreateSummary(IConfig config, bool hugeSd, Metric[] metrics)
            => CreateSummary<MockBenchmarkClass>(config, hugeSd, metrics);

        public static Summary CreateSummary<TBenchmark>(IConfig config, bool hugeSd, Metric[] metrics) => new Summary(
            "MockSummary",
            CreateBenchmarks<TBenchmark>(config).Select(b => CreateReport(b, hugeSd, metrics)).ToImmutableArray(),
            new HostEnvironmentInfoBuilder().Build(),
            string.Empty,
            string.Empty,
            TimeSpan.FromMinutes(1),
            TestCultureInfo.Instance,
            ImmutableArray<ValidationError>.Empty,
            ImmutableArray<IColumnHidingRule>.Empty);

        public static Summary CreateSummary<TBenchmark>(IConfig config, bool hugeSd, Func<BenchmarkCase, Metric[]> metricsBuilder) => new Summary(
            "MockSummary",
            CreateBenchmarks<TBenchmark>(config).Select(b => CreateReport(b, hugeSd, metricsBuilder(b))).ToImmutableArray(),
            new HostEnvironmentInfoBuilder().Build(),
            string.Empty,
            string.Empty,
            TimeSpan.FromMinutes(1),
            TestCultureInfo.Instance,
            ImmutableArray<ValidationError>.Empty,
            ImmutableArray<IColumnHidingRule>.Empty);

        public static SummaryStyle CreateSummaryStyle(bool printUnitsInHeader = false, bool printUnitsInContent = true, bool printZeroValuesInContent = false,
            SizeUnit? sizeUnit = null, TimeUnit? timeUnit = null, TextJustification textColumnJustification = TextJustification.Left,
            TextJustification numericColumnJustification = TextJustification.Left)
            => new SummaryStyle(DefaultCultureInfo.Instance, printUnitsInHeader, sizeUnit, timeUnit, printUnitsInContent: printUnitsInContent,
                printZeroValuesInContent: printZeroValuesInContent, textColumnJustification: textColumnJustification,
                numericColumnJustification: numericColumnJustification);

        private static ImmutableArray<BenchmarkReport> CreateReports(IConfig config)
            => CreateBenchmarks<MockBenchmarkClass>(config).Select(CreateSimpleReport).ToImmutableArray();

        private static BenchmarkCase[] CreateBenchmarks<TBenchmarks>(IConfig config)
            => BenchmarkConverter.TypeToBenchmarks(typeof(TBenchmarks), config).BenchmarksCases;

        private static BenchmarkReport CreateSimpleReport(BenchmarkCase benchmarkCase) => CreateReport(benchmarkCase, 1, 1);

        private static BenchmarkReport CreateReport(BenchmarkCase benchmarkCase, int n, double nanoseconds)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var measurements = Enumerable.Range(0, n)
                .Select(index => new Measurement(1, IterationMode.Workload, IterationStage.Result, index + 1, 1, nanoseconds + index).ToString())
                .ToList();
            var executeResult = new ExecuteResult(true, 0, default, measurements, new[] { $"// Runtime=extra output line" }, Array.Empty<string>(), 1);
            return new BenchmarkReport(true, benchmarkCase, buildResult, buildResult, new List<ExecuteResult> { executeResult }, Array.Empty<Metric>());
        }

        private static BenchmarkReport CreateReport(BenchmarkCase benchmarkCase, bool hugeSd, Metric[] metrics)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            bool isFoo = benchmarkCase.Descriptor.WorkloadMethodDisplayInfo == "Foo";
            bool isBar = benchmarkCase.Descriptor.WorkloadMethodDisplayInfo == "Bar";
            var measurements = new List<Measurement>
            {
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 2, 1, hugeSd && isFoo ? 2 : 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 3, 1, hugeSd && isBar ? 3 : 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 4, 1, hugeSd && isFoo ? 2 : 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 5, 1, hugeSd && isBar ? 3 : 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 6, 1, 1)
            };
            var executeResult = new ExecuteResult(measurements, default, default, 0);
            return new BenchmarkReport(true, benchmarkCase, buildResult, buildResult, new List<ExecuteResult> { executeResult }, metrics);
        }

        private static BenchmarkReport CreateReportWithBiasedDistribution(BenchmarkCase benchmarkCase, int min, int median, int max, int n, Metric[] metrics)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            bool isFoo = benchmarkCase.Descriptor.WorkloadMethodDisplayInfo == "Foo";
            bool isBar = benchmarkCase.Descriptor.WorkloadMethodDisplayInfo == "Bar";
            var measurements = from i in Enumerable.Range(0, Math.Max(1, n / 9))
                               from m in isFoo ? new[]
                               {
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, min), // 1
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 2, 1, min + ((median - min) / 2) + 1), // 3
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 4, 1, median), // 4
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 5, 1, median), // 4
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 5, 1, median), // 4
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 7, 1, median + ((max - median) / 2)), // 7
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 8, 1, median + ((max - median) / 2)), // 7
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 9, 1, max),    // 10
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 9, 1, max),    // 10
                               } : new[]
                               {
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, min), // 1
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, min), // 1
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 2, 1, min + ((median - min) / 2) + 1), // 3
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 2, 1, min + ((median - min) / 2) + 1), // 3
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 4, 1, median), // 4
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 5, 1, median), // 4
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 5, 1, median), // 4
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 7, 1, median + ((max - median) / 2)), // 7
                                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 9, 1, max),    // 10
                               }
                               select m;
            var executeResult = new ExecuteResult(measurements.Take(n).ToList(), default, default, 0);
            return new BenchmarkReport(true, benchmarkCase, buildResult, buildResult, new List<ExecuteResult> { executeResult }, metrics);
        }

        [LongRunJob]
        public class MockBenchmarkClass
        {
            [Benchmark] public void Foo() { }

            [Benchmark] public void Bar() { }
        }

        public static readonly Type MockType = typeof(MockBenchmarkClass);

        public static readonly MethodInfo MockMethodInfo = MockType.GetTypeInfo().GetMethods()
            .Single(method => method.Name == nameof(MockBenchmarkClass.Foo));
    }
}