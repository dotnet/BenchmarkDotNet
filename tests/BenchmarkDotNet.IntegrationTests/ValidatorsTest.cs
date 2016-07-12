using System.Collections.Generic;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ValidatorsTest
    {
        private readonly IExporter[] AllKnownExportersThatSupportExportToLog =
            {
                MarkdownExporter.Console,
                MarkdownExporter.Default,
                MarkdownExporter.GitHub,
                MarkdownExporter.StackOverflow,
                CsvExporter.Default,
                CsvMeasurementsExporter.Default,
                HtmlExporter.Default,
                PlainExporter.Default,
            };

        private readonly ITestOutputHelper output;

        public ValidatorsTest(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
        }

        [Fact]
        public void BenchmarkRunnerShouldNotFailOnCriticalValidationErrors()
        {
            BenchmarkRunner
                .Run<Nothing>(
                    ManualConfig
                        .CreateEmpty()
                        .With(new FailingValidator())
                        .With(ConsoleLogger.Default) // so we get an output in the TestRunner log
                        .With(new OutputLogger(output))
                        .With(AllKnownExportersThatSupportExportToLog));
        }

        [Fact]
        public void LoggersShouldNotFailOnCriticalValidationErrors()
        {
            var summary = BenchmarkRunner
                .Run<Nothing>(
                    ManualConfig
                        .CreateEmpty()
                        .With(ConsoleLogger.Default) // so we get an output in the TestRunner log
                        .With(new OutputLogger(output))
                        .With(new FailingValidator()));

            foreach (var exporter in AllKnownExportersThatSupportExportToLog)
            {
                exporter.ExportToLog(summary, new AccumulationLogger());
            }
        }

        private class FailingValidator : IValidator
        {
            public bool TreatsWarningsAsErrors => true;

            public IEnumerable<ValidationError> Validate(IList<Benchmark> benchmarks)
            {
                yield return new ValidationError(true, "It just fails");
            }
        }

        public class Nothing
        {
            [Benchmark]
            public void DoNothing() { }
        }
    }
}