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
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests.Attributes
{
    // In case of failed approval tests, use the following reporter:
    // [UseReporter(typeof(KDiffReporter))]
    [UseReporter(typeof(XUnit2Reporter))]
    [UseApprovalSubdirectory("ApprovedFiles")]
    [Collection("ApprovalTests")]
    public class ParamsAllValuesApprovalTests : IDisposable
    {
        private readonly CultureInfo initCulture;

        public ParamsAllValuesApprovalTests() => initCulture = Thread.CurrentThread.CurrentCulture;

        [UsedImplicitly]
        public static TheoryData<Type> GetBenchmarkTypes()
        {
            var data = new TheoryData<Type>();
            foreach (var type in typeof(Benchmarks).GetNestedTypes())
                data.Add(type);
            return data;
        }

        [Theory]
        [MemberData(nameof(GetBenchmarkTypes))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BenchmarkShouldProduceSummary(Type benchmarkType)
        {
            NamerFactory.AdditionalInformation = benchmarkType.Name;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var logger = new AccumulationLogger();
            logger.WriteLine("=== " + benchmarkType.Name + " ===");

            var exporter = MarkdownExporter.Mock;
            var summary = MockFactory.CreateSummary(benchmarkType);
            exporter.ExportToLog(summary, logger);

            var validator = ParamsAllValuesValidator.FailOnError;
            var errors = validator.Validate(new ValidationParameters(summary.BenchmarksCases, summary.BenchmarksCases.First().Config)).ToList();
            logger.WriteLine();
            logger.WriteLine("Errors: " + errors.Count);
            foreach (var error in errors)
                logger.WriteLineError("* " + error.Message);

            Approvals.Verify(logger.GetLog());
        }

        public void Dispose() => Thread.CurrentThread.CurrentCulture = initCulture;

        public enum TestEnum
        {
            A = 1, B, C
        }

        [Flags]
        public enum TestFlagsEnum
        {
            A = 0b001,
            B = 0b010,
            C = 0b100
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static class Benchmarks
        {
            public class WithAllValuesOfBool
            {
                [ParamsAllValues]
                public bool ParamProperty { get; set; }

                [Benchmark]
                public void Benchmark() { }
            }

            public class WithAllValuesOfEnum
            {
                [ParamsAllValues]
                public TestEnum ParamProperty { get; set; }

                [Benchmark]
                public void Benchmark() { }
            }

            public class WithAllValuesOfNullableBool
            {
                [ParamsAllValues]
                public bool? ParamProperty { get; set; }

                [Benchmark]
                public void Benchmark() { }
            }

            public class WithAllValuesOfNullableEnum
            {
                [ParamsAllValues]
                public TestEnum? ParamProperty { get; set; }

                [Benchmark]
                public void Benchmark() { }
            }

            public class WithNotAllowedTypeError
            {
                [ParamsAllValues]
                public int ParamProperty { get; set; }

                [Benchmark]
                public void Benchmark() { }
            }

            public class WithNotAllowedNullableTypeError
            {
                [ParamsAllValues]
                public int? ParamProperty { get; set; }

                [Benchmark]
                public void Benchmark() { }
            }

            public class WithNotAllowedFlagsEnumError
            {
                [ParamsAllValues]
                public TestFlagsEnum ParamProperty { get; set; }

                [Benchmark]
                public void Benchmark() { }
            }
        }
    }
}
