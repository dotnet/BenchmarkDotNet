using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class MarkdownExporter : ExporterBase
    {
        public enum MarkdownHighlightStrategy
        {
            // Don't highlight
            None,

            // Bold highlighting
            Bold,

            // Mark end of the line with special symbol (for tests)
            Marker
        }

        protected override string FileExtension => "md";
        protected override string FileNameSuffix => $"-{Dialect.ToLower()}";

        private string Dialect { get; set; }

        public static readonly IExporter Default = new MarkdownExporter
        {
            Dialect = nameof(Default),
            startOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold
        };

        public static readonly IExporter Console = new MarkdownExporter
        {
            Dialect = nameof(Console),
            startOfGroupHighlightStrategy = MarkdownHighlightStrategy.None
        };

        public static readonly IExporter StackOverflow = new MarkdownExporter
        {
            Dialect = nameof(StackOverflow),
            prefix = "    ",
            startOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold
        };

        public static readonly IExporter GitHub = new MarkdownExporter
        {
            Dialect = nameof(GitHub),
            useCodeBlocks = true,
            codeBlockStart = "``` ini",
            startOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold,
            columnsStartWithSeparator = true,
            escapeHtml = true
        };

        public static readonly IExporter Atlassian = new MarkdownExporter
        {
            Dialect = nameof(Atlassian),
            startOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold,
            tableHeaderSeparator = " ||",
            useHeaderSeparatingRow = false,
            columnsStartWithSeparator = true,
            useCodeBlocks = true,
            codeBlockStart = "{noformat}",
            codeBlockEnd = "{noformat}",
            boldMarkupFormat = "*{0}*"
        };

        // Only for unit tests
        internal static readonly IExporter Mock = new MarkdownExporter
        {
            Dialect = nameof(Mock),
            startOfGroupHighlightStrategy = MarkdownHighlightStrategy.Marker
        };

        private string prefix = string.Empty;
        private bool useCodeBlocks;
        private string codeBlockStart = "```";
        private string codeBlockEnd = "```";
        private MarkdownHighlightStrategy startOfGroupHighlightStrategy = MarkdownHighlightStrategy.None;
        private string tableHeaderSeparator = " |";
        private string tableColumnSeparator = " |";
        private bool useHeaderSeparatingRow = true;
        private bool columnsStartWithSeparator;
        private string boldMarkupFormat = "**{0}**";
        private bool escapeHtml;

        private MarkdownExporter() { }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            if (useCodeBlocks)
            {
                logger.WriteLine(codeBlockStart);
            }

            logger = GetRightLogger(logger);
            logger.WriteLine();
            foreach (string infoLine in summary.HostEnvironmentInfo.ToFormattedString())
            {
                logger.WriteLineInfo(infoLine);
            }

            logger.WriteLineInfo(summary.AllRuntimes);
            logger.WriteLine();

            PrintTable(summary.Table, logger);

            // TODO: move this logic to an analyser
            var benchmarksWithTroubles = summary.Reports.Where(r => !r.GetResultRuns().Any()).Select(r => r.BenchmarkCase).ToList();
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
                logger.Write(tableHeaderSeparator.TrimStart());
            }

            table.PrintLine(table.FullHeader, logger, string.Empty, tableHeaderSeparator);
            if (useHeaderSeparatingRow)
            {
                if (columnsStartWithSeparator)
                {
                    logger.Write(tableHeaderSeparator.TrimStart());
                }

                logger.WriteLineStatistic(string.Join("",
                    table.Columns.Where(c => c.NeedToShow).Select(column => new string('-', column.Width) + GetJustificationIndicator(column.Justify) + "|")));
            }

            int rowCounter = 0;
            bool highlightRow = false;
            var separatorLine = Enumerable.Range(0, table.ColumnCount).Select(_ => "").ToArray();
            foreach (var line in table.FullContent)
            {
                if (rowCounter > 0 && table.FullContentStartOfLogicalGroup[rowCounter] && table.SeparateLogicalGroups)
                {
                    // Print logical separator
                    if (columnsStartWithSeparator)
                        logger.Write(tableColumnSeparator.TrimStart());
                    table.PrintLine(separatorLine, logger, string.Empty, tableColumnSeparator, highlightRow, false, startOfGroupHighlightStrategy,
                        boldMarkupFormat, false);
                }

                // Each time we hit the start of a new group, alternative the colour (in the console) or display bold in Markdown
                if (table.FullContentStartOfHighlightGroup[rowCounter])
                {
                    highlightRow = !highlightRow;
                }

                if (columnsStartWithSeparator)
                    logger.Write(tableColumnSeparator.TrimStart());

                table.PrintLine(line, logger, string.Empty, tableColumnSeparator, highlightRow, table.FullContentStartOfHighlightGroup[rowCounter],
                    startOfGroupHighlightStrategy, boldMarkupFormat, escapeHtml);
                rowCounter++;
            }
        }

        private static string GetJustificationIndicator(SummaryTable.SummaryTableColumn.TextJustification textJustification)
        {
            switch (textJustification)
            {
                case SummaryTable.SummaryTableColumn.TextJustification.Left:
                    return " ";
                case SummaryTable.SummaryTableColumn.TextJustification.Right:
                    return ":";
                default:
                    return " ";
            }
        }
    }
}