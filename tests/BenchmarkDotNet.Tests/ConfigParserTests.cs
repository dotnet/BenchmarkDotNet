using System.Linq;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
{
    public class ConfigParserTests
    {
        public ITestOutputHelper Output { get; }

        public ConfigParserTests(ITestOutputHelper output) => Output = output;

        [Theory]
        [InlineData("--job=dry", "--exporters", "html", "rplot")]
        [InlineData("--JOB=dry", "--EXPORTERS", "html", "rplot")] // case insensitive
        [InlineData("-j", "dry", "-e", "html", "rplot")] // alias
        public void SimpleConfigParsedCorrectly(params string[] args)
        {
            var config = ConfigParser.Parse(args, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Contains(Job.Dry, config.GetJobs());

            Assert.Equal(3, config.GetExporters().Count()); // rplot must come together with CsvMeasurementsExporter
            Assert.Contains(HtmlExporter.Default, config.GetExporters());
            Assert.Contains(RPlotExporter.Default, config.GetExporters());
            Assert.Contains(CsvMeasurementsExporter.Default, config.GetExporters());

            Assert.Empty(config.GetColumnProviders());
            Assert.Empty(config.GetDiagnosers());
            Assert.Empty(config.GetAnalysers());
            Assert.Empty(config.GetLoggers());
        }

        [Fact]
        public void SimpleConfigAlternativeVersionParserCorrectly()
        {
            var config = ConfigParser.Parse(new[] { "--job=Dry" }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Contains(Job.Dry, config.GetJobs());
        }

        [Fact]
        public void UnknownConfigIsFailure()
        {
            Assert.False(ConfigParser.Parse(new[] { "--unknown" }, new OutputLogger(Output)).isSuccess);
        }
    }
}