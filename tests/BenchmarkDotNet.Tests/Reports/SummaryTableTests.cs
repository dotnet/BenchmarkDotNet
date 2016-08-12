using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Reports
{
    public class SummaryTableTests
    {
        private readonly ITestOutputHelper output;

        public SummaryTableTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private SummaryTable CreateTable()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance;
            var summary = MockFactory.CreateSummary(config);
            var table = new SummaryTable(summary);
            MarkdownExporter.Default.ExportToLog(summary, logger);
            output.WriteLine(logger.GetLog());
            return table;
        }

        private SummaryTable.SummaryTableColumn CreateColumn(string header)
        {
            var column = CreateTable().Columns.FirstOrDefault(c => c.Header == header);
            Assert.NotNull(column);
            return column;
        }

        [Fact]
        public void PlatformTest()
        {
            var gcModeColumn = CreateColumn("Platform");
            Assert.Equal(true, gcModeColumn.IsTrivial);
        }

        [Fact]
        public void GcModeTest()
        {
            var gcModeColumn = CreateColumn("GcMode");
            Assert.Equal(false, gcModeColumn.IsTrivial);
        }
    }
}