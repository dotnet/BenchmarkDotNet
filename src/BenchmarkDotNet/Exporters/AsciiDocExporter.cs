using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class AsciiDocExporter : ExporterBase
    {
        protected override string FileExtension => "asciidoc";

        public static readonly IExporter Default = new AsciiDocExporter();

        private AsciiDocExporter()
        {
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            logger.WriteLine("....");
            foreach (string infoLine in summary.HostEnvironmentInfo.ToFormattedString())
            {
                logger.WriteLineInfo(infoLine);
            }
            logger.WriteLineInfo(summary.AllRuntimes);
            logger.WriteLine();

            PrintTable(summary.Table, logger);

            var benchmarksWithTroubles = summary.Reports
                .Where(r => !r.GetResultRuns().Any())
                .Select(r => r.BenchmarkCase)
                .ToList();

            if (benchmarksWithTroubles.Count > 0)
            {
                logger.WriteLine();
                logger.WriteLine("[WARNING]");
                logger.WriteLineError(".Benchmarks with issues");
                logger.WriteLine("====");
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                    logger.WriteLineError("* " + benchmarkWithTroubles.DisplayInfo);
                logger.WriteLine("====");
            }
        }

        private static void PrintTable(SummaryTable table, ILogger logger)
        {
            if (table.FullContent.Length == 0)
            {
                logger.WriteLine("[WARNING]");
                logger.WriteLine("====");
                logger.WriteLineError("There are no benchmarks found ");
                logger.WriteLine("====");
                logger.WriteLine();
                return;
            }

            table.PrintCommonColumns(logger);
            logger.WriteLine("....");

            logger.WriteLine("[options=\"header\"]");
            logger.WriteLine("|===");
            table.PrintLine(table.FullHeader, logger, "|", string.Empty);
            foreach (var line in table.FullContent)
                table.PrintLine(line, logger, "|", string.Empty);
            logger.WriteLine("|===");
        }
    }
}