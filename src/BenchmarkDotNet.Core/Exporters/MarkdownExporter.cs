using System.Linq;
using BenchmarkDotNet.Environments;
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
            Dialect = nameof(Default),
            startOfGroupInBold = true
        };

        public static readonly IExporter Console = new MarkdownExporter
        {
            Dialect = nameof(Console),
            startOfGroupInBold = false
        };

        public static readonly IExporter StackOverflow = new MarkdownExporter
        {
            Dialect = nameof(StackOverflow),
            prefix = "    ",
            startOfGroupInBold = true
        };

        public static readonly IExporter GitHub = new MarkdownExporter
        {
            Dialect = nameof(GitHub),
            useCodeBlocks = true,
            codeBlockStart = "``` ini",
            startOfGroupInBold = true
        };

        public static readonly IExporter Atlassian = new MarkdownExporter
        {
            Dialect = nameof(Atlassian),
            startOfGroupInBold = true,
            tableHeaderSeparator = " ||",
            useHeaderSeparatingRow = false,
            columnsStartWithSeparator = true,
            useCodeBlocks = true,
            codeBlockStart = "{noformat}",
            codeBlockEnd = "{noformat}"
        };

        private string prefix = string.Empty;
        private bool useCodeBlocks = false;
        private string codeBlockStart = "```";
        private string codeBlockEnd = "```";
        private bool startOfGroupInBold = false;
        private string tableHeaderSeparator = " |";
        private string tableColumnSeparator = " |";
        private bool useHeaderSeparatingRow = true;
        private bool columnsStartWithSeparator = false;

        private MarkdownExporter()
        {
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            if (useCodeBlocks)
            {
                logger.WriteLine(codeBlockStart);
            }
            logger = GetRightLogger(logger);
            logger.WriteLine();
            foreach (var infoLine in HostEnvironmentInfo.GetCurrent().ToFormattedString())
            {
                logger.WriteLineInfo(infoLine);
            }
            logger.WriteLineInfo(summary.JobRuntimes);
            logger.WriteLine();

            PrintTable(summary.Table, logger);

            // TODO: move this logic to an analyser
            var benchmarksWithTroubles = summary.Reports.Where(r => !r.GetResultRuns().Any()).Select(r => r.Benchmark).ToList();
            if (benchmarksWithTroubles.Count > 0)
            {
                logger.WriteLine();
                logger.WriteLineError("Benchmarks with issues:");
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                {
                    logger.WriteLineError("  " + benchmarkWithTroubles.DisplayInfo);
                }
            }
        }

        private ILogger GetRightLogger(ILogger logger)
        {
            if (string.IsNullOrEmpty(prefix)) // most common scenario!! we don't need expensive LoggerWithPrefix
            {
                return logger;
            }

            return new LoggerWithPrefix(logger, prefix);
        }

        private void PrintTable(SummaryTable table, ILogger logger)
        {
            if (table.FullContent.Length == 0)
            {
                logger.WriteLineError("There are no benchmarks found ");
                logger.WriteLine();
                return;
            }

            table.PrintCommonColumns(logger);
            logger.WriteLine();

            if (useCodeBlocks)
            {
                logger.Write(codeBlockEnd);
                logger.WriteLine();
            }

            if (columnsStartWithSeparator)
            {
                logger.Write(tableHeaderSeparator);
            }

            table.PrintLine(table.FullHeader, logger, string.Empty, tableHeaderSeparator);
            if (useHeaderSeparatingRow)
            {
                logger.WriteLineStatistic(string.Join("", table.Columns.Where(c => c.NeedToShow).Select(c => new string('-', c.Width) + " |")));
            }
            var rowCounter = 0;
            var highlightRow = false;
            foreach (var line in table.FullContent)
            {
                // Each time we hit the start of a new group, alternative the colour (in the console) or display bold in Markdown
                if (table.FullContentStartOfGroup[rowCounter])
                {
                    highlightRow = !highlightRow;
                }

                if (columnsStartWithSeparator)
                {
                    logger.Write(tableColumnSeparator);
                }

                table.PrintLine(line, logger, string.Empty, tableColumnSeparator, highlightRow, table.FullContentStartOfGroup[rowCounter], startOfGroupInBold);
                rowCounter++;
            }
        }
    }
}