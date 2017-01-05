using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    // See https://github.com/dotnet/BenchmarkDotNet/issues/172
    public class ExporterDependencyTests
    {
        [Fact]
        public void MissingDependencyIsAdded()
        {
            var compositeExporter = new CompositeExporter(TestExporter.Default);
            Assert.Equal(2, compositeExporter.exporters.Count());
            Assert.Equal(new IExporter[] { TestExporter.Default, TestExporterDependency.Default }, compositeExporter.exporters);
        }

        [Fact]
        public void MissingDependencyIsNotAddedWhenItIsAlreadyPresent()
        {
            var compositeExporter = new CompositeExporter(TestExporter.Default, TestExporterDependency.Default);
            Assert.Equal(2, compositeExporter.exporters.Count());
            Assert.Equal(new IExporter[] { TestExporter.Default, TestExporterDependency.Default }, compositeExporter.exporters);
        }
    }

    public class TestExporter : IExporter, IExporterDependencies
    {
        public static readonly TestExporter Default = new TestExporter();

        public IEnumerable<IExporter> Dependencies
        {
            get { yield return TestExporterDependency.Default; }
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger) => Enumerable.Empty<string>();

        public void ExportToLog(Summary summary, ILogger logger) { }
    }

    public class TestExporterDependency : IExporter
    {
        public static readonly TestExporterDependency Default = new TestExporterDependency();

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger) => Enumerable.Empty<string>();

        public void ExportToLog(Summary summary, ILogger logger) { }
    }
}
