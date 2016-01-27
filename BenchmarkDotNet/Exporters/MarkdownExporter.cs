using System.Linq;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class MarkdownExporter : ExporterBase
    {
        protected override string FileExtension => "md";
        protected override string FileNameSuffix => $"-{Dialect.ToLower()}";

        private string Dialect { get; set; }

        public static readonly IExporter Default = new MarkdownExporter
        {
            Dialect = nameof(Default)
        };

        public static readonly IExporter StackOverflow = new MarkdownExporter
        {
            prefix = "    ",
            Dialect = nameof(StackOverflow)
        };

        public static readonly IExporter GitHub = new MarkdownExporter
        {
            Dialect = nameof(GitHub),
            useCodeBlocks = true,
            codeBlocksSyntax = "ini"
        };

        private string prefix = string.Empty;
        private bool useCodeBlocks = false;
        private string codeBlocksSyntax = string.Empty;

        private MarkdownExporter()
        {
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            if (useCodeBlocks)
                logger.WriteLine($"```{codeBlocksSyntax}");
            logger = new LoggerWithPrefix(logger, prefix);
            logger.WriteLineInfo(EnvironmentHelper.GetCurrentInfo().ToFormattedString("Host"));
            logger.NewLine();

            PrintTable(summary.Table, logger);

            // TODO: move this logic to an analyser
            var benchmarksWithTroubles = summary.Reports.Values.Where(r => !r.GetTargetRuns().Any()).Select(r => r.Benchmark).ToList();
            if (benchmarksWithTroubles.Count > 0)
            {
                logger.NewLine();
                logger.WriteLineError("Benchmarks with troubles:");
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                    logger.WriteLineError("  " + benchmarkWithTroubles.ShortInfo);
            }
        }

        private void PrintTable(SummaryTable table, ILogger logger)
        {
            if (table.FullContent.Length == 0)
            {
                logger.WriteLineError("There are no found benchmarks");
                logger.NewLine();
                return;
            }
            PrintCommonColumns(table, logger);

            if (useCodeBlocks)
            {
                logger.Write("```");
                logger.NewLine();
            }

            PrintLine(table, table.FullHeader, logger);
            logger.WriteLineStatistic(string.Join("", table.Columns.Where(c => c.NeedToShow).Select(c => new string('-', c.Width) + " |")));
            foreach (var line in table.FullContent)
                PrintLine(table, line, logger);
        }

        private static void PrintCommonColumns(SummaryTable table, ILogger logger)
        {
            var commonColumns = table.Columns.Where(c => !c.NeedToShow && !c.IsTrivial).ToArray();
            if (commonColumns.Any())
            {
                var paramsOnLine = 0;
                foreach (var column in commonColumns)
                {
                    logger.WriteInfo($"{column.Header}={column.Content[0]}  ");
                    paramsOnLine++;
                    if (paramsOnLine == 3)
                    {
                        logger.NewLine();
                        paramsOnLine = 0;
                    }
                }
                if (paramsOnLine != 0)
                    logger.NewLine();

                logger.NewLine();
            }
        }

        private void PrintLine(SummaryTable table, string[] line, ILogger logger)
        {
            for (int columnIndex = 0; columnIndex < table.ColumnCount; columnIndex++)
                if (table.Columns[columnIndex].NeedToShow)
                    logger.WriteStatistic(line[columnIndex].PadLeft(table.Columns[columnIndex].Width, ' ') + " |");
            logger.NewLine();

        }
    }
}