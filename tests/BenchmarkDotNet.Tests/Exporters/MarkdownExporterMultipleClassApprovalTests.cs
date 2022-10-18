using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests.Exporters
{
    [UseReporter(typeof(XUnit2Reporter))]
    [UseApprovalSubdirectory("ApprovedFiles")]
    [Collection("ApprovalTests")]
    public class MarkdownExporterMultipleClassApprovalTests : IDisposable
    {
        private readonly CultureInfo initCulture;

        public MarkdownExporterMultipleClassApprovalTests() => initCulture = Thread.CurrentThread.CurrentCulture;

        [UsedImplicitly]
        public static TheoryData<Type, BenchmarkLogicalGroupRule?> GetLogicalGroupRules()
        {
            var data = new TheoryData<Type, BenchmarkLogicalGroupRule?>();

            foreach (var type in typeof(LogicalGroupBenchmarks).GetNestedTypes())
            {
                data.Add(type, null); // no logical rule

                foreach (var rule in Enum.GetValues(typeof(BenchmarkLogicalGroupRule)))
                    data.Add(type, (BenchmarkLogicalGroupRule?)rule);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(GetLogicalGroupRules))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogicalRuleTest(Type benchmarkType, BenchmarkLogicalGroupRule? rule)
        {
            var config = DefaultConfig.Instance;
            if (rule is { } logicalRule)
                config = config.AddLogicalGroupRules(logicalRule);

            var fileName = rule == null
                ? $"{benchmarkType.Name}.Default"
                : $"{benchmarkType.Name}.{rule}";

            NamerFactory.AdditionalInformation = fileName;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var logger = new AccumulationLogger();
            logger.WriteLine($"=== " + fileName + " ===");

            var exporter = MarkdownExporter.Mock;
            var summary = MockFactory.CreateSummary(config, benchmarkType.GetNestedTypes());
            exporter.ExportToLog(summary, logger);

            var validator = BaselineValidator.FailOnError;
            var errors = validator.Validate(new ValidationParameters(summary.BenchmarksCases, summary.BenchmarksCases.First().Config)).ToList();
            logger.WriteLine();
            logger.WriteLine("Errors: " + errors.Count);
            foreach (var error in errors)
                logger.WriteLineError("* " + error.Message);

            Approvals.Verify(logger.GetLog());
        }

        public void Dispose() => Thread.CurrentThread.CurrentCulture = initCulture;

        public static class LogicalGroupBenchmarks
        {
            public static class NoBaseline
            {
                [LogicalGroupColumn, CategoriesColumn, BaselineColumn]
                [BenchmarkCategory("A")]
                public class Bench1
                {
                    [Params(10, 20)] public int Param;
                    [Benchmark] public void Foo() { }
                    [Benchmark] public void Bar() { }
                }

                [BenchmarkCategory("B")]
                public class Bench2
                {
                    [Params(10, 20)] public int Param;
                    [Benchmark] public void Foo() { }
                    [Benchmark] public void Bar() { }
                }
            }

            public static class OneBaseline
            {
                [LogicalGroupColumn, CategoriesColumn, BaselineColumn]
                [BenchmarkCategory("A")]
                public class Bench1
                {
                    [Params(10, 20)] public int Param;
                    [Benchmark(Baseline = true)] public void Foo() { }
                    [Benchmark] public void Bar() { }
                }

                [BenchmarkCategory("B")]
                public class Bench2
                {
                    [Params(10, 20)] public int Param;
                    [Benchmark] public void Foo() { }
                    [Benchmark] public void Bar() { }
                }
            }
        }
    }
}