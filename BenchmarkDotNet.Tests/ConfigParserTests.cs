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
        [Fact]
        public void SimpleConfigParsedCorrectly()
        {
            var parser = new ConfigParser();
            // We allow args with and without the double dashes (i.e. '--jobs=' and 'jobs=')
            var config = parser.Parse(new[] { "--jobs=dry", "exporters=html,rplot" });

            Assert.Equal(1, config.GetJobs().Count());
            Assert.Contains(Job.Dry, config.GetJobs());

            Assert.Equal(2, config.GetExporters().Count());
            Assert.Contains(HtmlExporter.Default, config.GetExporters());
            Assert.Contains(RPlotExporter.Default, config.GetExporters());

            Assert.Equal(0, config.GetColumns().Count());
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

        [Fact]
        public void ConfigWithAllOptionParsedCorrectly()
        {
            var parser = new ConfigParser();
            var config = parser.Parse(new[] { "jobs=all" });

            // TODO How to make this robust, 11 is only valid when there are 11 items in "availableJobs" in ConfigParser.cs
            Assert.Equal(11, config.GetJobs().Count());

            Assert.Equal(0, config.GetColumns().Count());
            Assert.Equal(0, config.GetExporters().Count());
            Assert.Equal(0, config.GetDiagnosers().Count());
            Assert.Equal(0, config.GetAnalysers().Count());
            Assert.Equal(0, config.GetLoggers().Count());
        }
    }
}
