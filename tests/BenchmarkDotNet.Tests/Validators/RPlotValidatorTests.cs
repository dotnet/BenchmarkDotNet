using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Validators
{
    public class RPlotValidatorTests
    {
        [Fact]
        public void RPlotExporterRequiresDefaultCsvExporter()
        {
            var actualSeparator = Thread.CurrentThread.CurrentCulture.GetActualListSeparator();

            var alternativeSeparator = actualSeparator == CsvSeparator.Comma.ToRealSeparator()
                ? CsvSeparator.Semicolon
                : CsvSeparator.Comma;

            var config = ManualConfig.CreateEmpty();
            config.AddExporter(new CsvMeasurementsExporter(alternativeSeparator));
            config.AddExporter(RPlotExporter.Default);

            var validationParameters = new ValidationParameters(ImmutableArray<BenchmarkCase>.Empty, config.CreateImmutableConfig());
            var validationErrors = RPlotExporterValidator.FailOnError.Validate(validationParameters).ToArray();

            Assert.Single(validationErrors);
            Assert.Equal("RPlotExporter requires CsvMeasurementsExporter.Default. Do not override CsvMeasurementsExporter", validationErrors.First().Message);
        }
    }
}