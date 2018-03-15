﻿#if CLASSIC || NETCOREAPP2_0
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests.Exporters
{
    // In case of failed approval tests, use the following reporter:
    // [UseReporter(typeof(KDiffReporter))]
    [UseReporter(typeof(XUnit2Reporter))]
    [UseApprovalSubdirectory("ApprovedFiles")]
    [Collection("ApprovalTests")]
    public class MarkdownExporterApprovalTests : IDisposable
    {
        private readonly CultureInfo initCulture;

        public MarkdownExporterApprovalTests() => initCulture = Thread.CurrentThread.CurrentCulture;

        [UsedImplicitly]
        public static TheoryData<Type> GetGroupBenchmarkTypes()
        {
            var data = new TheoryData<Type>();
            foreach (var type in typeof(BaselinesBenchmarks).GetNestedTypes())
                data.Add(type);
            return data;
        }

        [Theory]
        [MemberData(nameof(GetGroupBenchmarkTypes))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GroupExporterTest(Type benchmarkType)
        {
            NamerFactory.AdditionalInformation = benchmarkType.Name;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var logger = new AccumulationLogger();
            logger.WriteLine("=== " + benchmarkType.Name + " ===");
            
            var exporter = MarkdownExporter.Mock;
            var summary = MockFactory.CreateSummary(benchmarkType);
            exporter.ExportToLog(summary, logger);
            
            var validator = BaselineValidator.FailOnError;
            var errors = validator.Validate(new ValidationParameters(summary.Benchmarks, summary.Config)).ToList();
            logger.WriteLine();
            logger.WriteLine("Errors: " + errors.Count);
            foreach (var error in errors) 
                logger.WriteLineError("* " + error.Message);

            Approvals.Verify(logger.GetLog());
        }

        public void Dispose() => Thread.CurrentThread.CurrentCulture = initCulture;

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static class BaselinesBenchmarks
        {
            /* NoBaseline */

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            public class NoBasline_MethodsParamsJobs
            {
                [Params(2, 10), UsedImplicitly] public int Param;

                [Benchmark] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
            public class NoBasline_MethodsParamsJobs_GroupByMethod
            {
                [Params(2, 10), UsedImplicitly] public int Param;

                [Benchmark, BenchmarkCategory("CatA")] public void Base() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Foo() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByJob)]
            public class NoBasline_MethodsParamsJobs_GroupByJob
            {
                [Params(2, 10), UsedImplicitly] public int Param;

                [Benchmark, BenchmarkCategory("CatA")] public void Base() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Foo() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
            public class NoBasline_MethodsParamsJobs_GroupByParams
            {
                [Params(2, 10), UsedImplicitly] public int Param;

                [Benchmark, BenchmarkCategory("CatA")] public void Base() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Foo() { }
                [Benchmark, BenchmarkCategory("CatB")] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
            public class NoBasline_MethodsParamsJobs_GroupByCategory
            {
                [Params(2, 10), UsedImplicitly] public int Param;

                [Benchmark(Baseline = true), BenchmarkCategory("CatA")]
                public void A1() { }

                [Benchmark, BenchmarkCategory("CatA")] public void A2() { }

                [Benchmark(Baseline = true), BenchmarkCategory("CatB")]
                public void B1() { }

                [Benchmark, BenchmarkCategory("CatB")] public void B2() { }
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            [GroupBenchmarksBy(
                BenchmarkLogicalGroupRule.ByMethod,
                BenchmarkLogicalGroupRule.ByJob,
                BenchmarkLogicalGroupRule.ByParams,
                BenchmarkLogicalGroupRule.ByCategory)]
            public class NoBasline_MethodsParamsJobs_GroupByAll
            {
                [Params(2, 10), UsedImplicitly] public int Param;

                [Benchmark(Baseline = true), BenchmarkCategory("CatA")]
                public void A1() { }

                [Benchmark, BenchmarkCategory("CatA")] public void A2() { }

                [Benchmark(Baseline = true), BenchmarkCategory("CatB")]
                public void B1() { }

                [Benchmark, BenchmarkCategory("CatB")] public void B2() { }
            }

            /* MethodBasline */

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            public class MethodBasline_Methods
            {
                [Benchmark(Baseline = true)] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            public class MethodBasline_MethodsParams
            {
                [Params(2, 10), UsedImplicitly] public int Param;

                [Benchmark(Baseline = true)] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            public class MethodBasline_MethodsJobs
            {
                [Benchmark(Baseline = true)] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1"), SimpleJob(id: "Job2")]
            public class MethodBasline_MethodsParamsJobs
            {
                [Params(2, 10), UsedImplicitly] public int Param;

                [Benchmark(Baseline = true)] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            /* JobBaseline */

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1", isBaseline: true), SimpleJob(id: "Job2")]
            public class JobBasline_MethodsJobs
            {
                [Benchmark] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1", isBaseline: true), SimpleJob(id: "Job2")]
            public class JobBasline_MethodsParamsJobs
            {
                [Params(2, 10), UsedImplicitly] public int Param;

                [Benchmark] public void Base() { }
                [Benchmark] public void Foo() { }
                [Benchmark] public void Bar() { }
            }
            
            /* Invalid */

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            public class Invalid_TwoMethodBaselines
            {
                [Benchmark(Baseline = true)] public void Foo() {}
                [Benchmark(Baseline = true)] public void Bar() {}
            }

            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1", isBaseline: true), SimpleJob(id: "Job2", isBaseline: true)]
            public class Invalid_TwoJobBaselines
            {
                [Benchmark] public void Foo() {}
                [Benchmark] public void Bar() {}
            }
            
            [RankColumn, LogicalGroupColumn, IsBaselineColumn]
            [SimpleJob(id: "Job1", isBaseline: true), SimpleJob(id: "Job2")]
            public class Invalid_MethodAndJobBaselines
            {
                [Benchmark(Baseline = true)] public void Foo() {}
                [Benchmark] public void Bar() {}
            }
        }
    }
}
#endif