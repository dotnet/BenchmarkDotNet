﻿using System;
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
using System.Collections.Generic;

namespace BenchmarkDotNet.Tests.Attributes
{
    // In case of failed approval tests, use the following reporter:
    // [UseReporter(typeof(KDiffReporter))]
    [UseReporter(typeof(XUnit2Reporter))]
    [UseApprovalSubdirectory("ApprovedFiles")]
    [Collection("ApprovalTests")]
    public class CustomEnvironmentInfoApprovalTests : IDisposable
    {
        private readonly CultureInfo initCulture;

        public CustomEnvironmentInfoApprovalTests() => initCulture = Thread.CurrentThread.CurrentCulture;

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

            var validator = CustomEnvironmentInfoValidator.FailOnError;
            var errors = validator.Validate(new ValidationParameters(summary.BenchmarksCases, summary.Config)).ToList();
            logger.WriteLine();
            logger.WriteLine("Errors: " + errors.Count);
            foreach (var error in errors)
                logger.WriteLineError("* " + error.Message);

            Approvals.Verify(logger.GetLog());
        }

        public void Dispose() => Thread.CurrentThread.CurrentCulture = initCulture;

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static class Benchmarks
        {
            public class WithSingleCustomLine
            {
                [CustomEnvironmentInfo]
                public static string CustomLine() => "Single line";

                [Benchmark] public void Base() { }
            }

            public class WithSequenceOfCustomLines
            {
                [CustomEnvironmentInfo]
                public static IEnumerable<string> SequenceOfCustomLines()
                {
                    yield return "First line";
                    yield return "Second line";
                }

                [Benchmark] public void Base() { }
            }

            public class WithArrayOfCustomLines
            {
                [CustomEnvironmentInfo]
                public static string[] ArrayOfCustomLines() =>
                    new[] { "First line", "Second line" };

                [Benchmark] public void Base() { }
            }

            public class WithErrorOnPrivateMethod
            {
                [CustomEnvironmentInfo]
                private static string CustomLine() => "Single line";

                [Benchmark] public void Base() { }
            }

            public class WithErrorOnNonStaticMethod
            {
                [CustomEnvironmentInfo]
                public string CustomLine() => "Single line";

                [Benchmark] public void Base() { }
            }

            public class WithErrorOnMethodWithParameters
            {
                [CustomEnvironmentInfo]
                public static string CustomLine(object o) => o.ToString();

                [Benchmark] public void Base() { }
            }

            public class WithErrorOnWrongReturnType
            {
                [CustomEnvironmentInfo]
                public static int CustomLine() => 42;

                [Benchmark] public void Base() { }
            }
        }
    }
}
