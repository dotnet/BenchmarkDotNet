using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    // See https://github.com/dotnet/BenchmarkDotNet/issues/172
    public class ExporterDependancyTests
    {
        [Fact]
        public void MissingDependancyIsAdded()
        {
            var compositeExporter = new CompositeExporter(TestExporter.Default);
            Assert.Equal(2, compositeExporter.exporters.Count());
            Assert.Equal(new IExporter[] { TestExporter.Default, TestExporterDependancy.Default }, compositeExporter.exporters);
        }

        [Fact]
        public void MissingDependancyIsNotAddedWhenItIsAlreadyPresent()
        {
            var compositeExporter = new CompositeExporter(TestExporter.Default, TestExporterDependancy.Default);
            Assert.Equal(2, compositeExporter.exporters.Count());
            Assert.Equal(new IExporter[] { TestExporter.Default, TestExporterDependancy.Default }, compositeExporter.exporters);
        }
    }

    public class TestExporter : IExporter, IExporterDependancies
    {
        public static readonly TestExporter Default = new TestExporter();

        public IEnumerable<IExporter> Dependencies
        {
            get { yield return TestExporterDependancy.Default; }
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger) => Enumerable.Empty<string>();

        public void ExportToLog(Summary summary, ILogger logger) { }
    }

    public class TestExporterDependancy : IExporter
    {
        public static readonly TestExporterDependancy Default = new TestExporterDependancy();

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger) => Enumerable.Empty<string>();

        public void ExportToLog(Summary summary, ILogger logger) { }
    }
}
