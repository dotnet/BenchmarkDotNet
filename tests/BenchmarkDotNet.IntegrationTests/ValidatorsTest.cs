using System.Collections.Generic;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Loggers;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Xml;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ValidatorsTest : BenchmarkTestExecutor
    {
        public ValidatorsTest(ITestOutputHelper output) : base(output) { }

        private readonly IExporter[] AllKnownExportersThatSupportExportToLog =
            {
                MarkdownExporter.Atlassian,
                MarkdownExporter.Console,
                MarkdownExporter.Default,
                MarkdownExporter.GitHub,
                MarkdownExporter.StackOverflow,
                CsvExporter.Default,
                CsvMeasurementsExporter.Default,
                HtmlExporter.Default,
                PlainExporter.Default,
                JsonExporter.Default,
                XmlExporter.Default
            };

        [Fact]
        public void BenchmarkRunnerShouldNotFailOnCriticalValidationErrors()
        {
            CanExecute<Nothing>(
                CreateSimpleConfig()
                        .AddValidator(new FailingValidator())
                        .AddExporter(AllKnownExportersThatSupportExportToLog),
                fullValidation: false);
        }

        [Fact]
        public void LoggersShouldNotFailOnCriticalValidationErrors()
        {
            var summary = CanExecute<Nothing>(CreateSimpleConfig().AddValidator(new FailingValidator()), fullValidation: false);

            foreach (var exporter in AllKnownExportersThatSupportExportToLog)
            {
                exporter.ExportToLog(summary, new AccumulationLogger());
            }
        }

        private class FailingValidator : IValidator
        {
            public bool TreatsWarningsAsErrors => true;

            public IEnumerable<ValidationError> Validate(ValidationParameters input)
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