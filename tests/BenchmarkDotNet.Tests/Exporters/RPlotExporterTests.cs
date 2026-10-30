using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Mocks;

namespace BenchmarkDotNet.Tests.Exporters
{
    public class RPlotExporterTests
    {
        private const string CustomScript = "# custom plotting script";

        [Fact]
        public async Task DefaultScriptIsWritten()
        {
            string script = await ExportAndReadScriptAsync(new RPlotExporter());

            Assert.Contains("BenchmarkDotNet", script);
            Assert.DoesNotContain("$CsvSeparator$", script);
        }

        [Fact]
        public async Task CustomScriptIsWrittenInsteadOfTheDefaultOne()
        {
            string script = await ExportAndReadScriptAsync(new CustomScriptRPlotExporter());

            Assert.Equal(CustomScript, script);
        }

        private static async Task<string> ExportAndReadScriptAsync(RPlotExporter exporter)
        {
            var resultsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(resultsDirectory);

            try
            {
                var summary = MockFactory.CreateSummary(MockFactory.MockType, resultsDirectory);

                try
                {
                    await exporter.ExportAsync(summary, new AccumulationLogger(), CancellationToken.None);
                }
                catch (ApplicationException)
                {
                    // Rscript is installed and failed on the mock summary, which has no measurements CSV file
                }

                return File.ReadAllText(Path.Combine(resultsDirectory, "BuildPlots.R"));
            }
            finally
            {
                Directory.Delete(resultsDirectory, recursive: true);
            }
        }

        private class CustomScriptRPlotExporter : RPlotExporter
        {
            protected override ValueTask<string> GetScriptAsync(Summary summary, CancellationToken cancellationToken)
                => new(CustomScript);
        }
    }
}
