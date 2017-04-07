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

            Assert.Equal(1, config.GetJobs().Count());
            Assert.Contains(Job.Dry, config.GetJobs());

            Assert.Equal(2, config.GetExporters().Count());
            Assert.Contains(HtmlExporter.Default, config.GetExporters());
            Assert.Contains(RPlotExporter.Default, config.GetExporters());

            Assert.Equal(0, config.GetColumnProviders().Count());
            Assert.Equal(0, config.GetDiagnosers().Count());
            Assert.Equal(0, config.GetAnalysers().Count());
            Assert.Equal(0, config.GetLoggers().Count());
        }

        [Fact]
        public void SimpleConfigAlternativeVersionParserCorrectly()
        {
            var parser = new ConfigParser();
            // To make it easier, we allow "jobs" and "job", 
            // plus casing doesn't matter, so "Dry" and "dry" are both valid
            var config = parser.Parse(new[] { "job=Dry" });

            Assert.Equal(1, config.GetJobs().Count());
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