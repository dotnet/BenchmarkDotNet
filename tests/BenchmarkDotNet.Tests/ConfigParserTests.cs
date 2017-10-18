using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ConfigParserTests
    {
        [Theory]
        [InlineData("--jobs=dry", "exporters=html,rplot")]
        [InlineData("--jobs=dry", "exporters", "html,rplot")]
        public void SimpleConfigParsedCorrectly(params string[] args)
        {
            var parser = new ConfigParser();
            // We allow args with and without the double dashes (i.e. '--jobs=' and 'jobs=')
            var config = parser.Parse(args);

            Assert.Single(config.GetJobs());
            Assert.Contains(Job.Dry, config.GetJobs());

            Assert.Equal(2, config.GetExporters().Count());
            Assert.Contains(HtmlExporter.Default, config.GetExporters());
            Assert.Contains(RPlotExporter.Default, config.GetExporters());

            Assert.Empty(config.GetColumnProviders());
            Assert.Empty(config.GetDiagnosers());
            Assert.Empty(config.GetAnalysers());
            Assert.Empty(config.GetLoggers());
        }

        [Fact]
        public void SimpleConfigAlternativeVersionParserCorrectly()
        {
            var parser = new ConfigParser();
            // To make it easier, we allow "jobs" and "job", 
            // plus casing doesn't matter, so "Dry" and "dry" are both valid
            var config = parser.Parse(new[] { "job=Dry" });

            Assert.Single(config.GetJobs());
            Assert.Contains(Job.Dry, config.GetJobs());
        }

        [Fact]
        public void UnknownConfigThrows()
        {
            var parser = new ConfigParser();
            Assert.Throws<InvalidOperationException>(() => parser.Parse(new[] { "jobs=unknown" }));
        }
    }
}