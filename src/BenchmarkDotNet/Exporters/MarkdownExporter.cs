using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

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
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold
        };

        public static readonly IExporter Console = new MarkdownExporter
        {
            Dialect = nameof(Console),
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.None,
            ColumnsStartWithSeparator = true // we want to be able to copy-paste the console output to GH #1062
        };

        public static readonly IExporter StackOverflow = new MarkdownExporter
        {
            Dialect = nameof(StackOverflow),
            Prefix = "    ",
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold
        };

        public static readonly IExporter GitHub = new MarkdownExporter
        {
            Dialect = nameof(GitHub),
            UseCodeBlocks = true,
            CodeBlockStart = "``` ini",
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold,
            ColumnsStartWithSeparator = true,
            EscapeHtml = true
        };

        public static readonly IExporter Atlassian = new MarkdownExporter
        {
            Dialect = nameof(Atlassian),
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold,
            TableHeaderSeparator = " ||",
            UseHeaderSeparatingRow = false,
            ColumnsStartWithSeparator = true,
            UseCodeBlocks = true,
            CodeBlockStart = "{noformat}",
            CodeBlockEnd = "{noformat}",
            BoldMarkupFormat = "*{0}*"
        };

        // Only for unit tests
        internal static readonly IExporter Mock = new MarkdownExporter
        {
            Dialect = nameof(Mock),
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Marker
        };

        [PublicAPI] protected string Prefix = string.Empty;
        [PublicAPI] protected bool UseCodeBlocks;
        [PublicAPI] protected string CodeBlockStart = "```";
        [PublicAPI] protected string CodeBlockEnd = "```";
        [PublicAPI] protected MarkdownHighlightStrategy StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.None;
        [PublicAPI] protected string TableHeaderSeparator = " |";
        [PublicAPI] protected string TableColumnSeparator = " |";
        [PublicAPI] protected bool UseHeaderSeparatingRow = true;
        [PublicAPI] protected bool ColumnsStartWithSeparator;
        [PublicAPI] protected string BoldMarkupFormat = "**{0}**";
        [PublicAPI] protected bool EscapeHtml;

        private MarkdownExporter() { }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            if (UseCodeBlocks)
            {
                logger.WriteLine(CodeBlockStart);
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

            // TODO: move this logic to an analyzer
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
            if (string.IsNullOrEmpty(Prefix)) // most common scenario!! we don't need expensive LoggerWithPrefix
            {
                return logger;
            }

            return new LoggerWithPrefix(logger, Prefix);
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

            if (table.Columns.All(c => !c.NeedToShow))
            {
                logger.WriteLine();
                logger.WriteLine("There are no columns to show ");
                return;
            }

            logger.WriteLine();

            if (UseCodeBlocks)
            {
                logger.Write(CodeBlockEnd);
                logger.WriteLine();
            }

            if (ColumnsStartWithSeparator)
            {
                logger.WriteStatistic(TableHeaderSeparator.TrimStart());
            }

            table.PrintLine(table.FullHeader, logger, string.Empty, TableHeaderSeparator);
            if (UseHeaderSeparatingRow)
            {
                if (ColumnsStartWithSeparator)
                {
                    logger.WriteStatistic(TableHeaderSeparator.TrimStart());
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
                    if (ColumnsStartWithSeparator)
                        logger.WriteStatistic(TableColumnSeparator.TrimStart());
                    table.PrintLine(separatorLine, logger, string.Empty, TableColumnSeparator, highlightRow, false, StartOfGroupHighlightStrategy,
                        BoldMarkupFormat, false);
                }

                // Each time we hit the start of a new group, alternative the color (in the console) or display bold in Markdown
                if (table.FullContentStartOfHighlightGroup[rowCounter])
                {
                    highlightRow = !highlightRow;
                }

                if (ColumnsStartWithSeparator)
                    logger.WriteStatistic(TableColumnSeparator.TrimStart());

                table.PrintLine(line, logger, string.Empty, TableColumnSeparator, highlightRow, table.FullContentStartOfHighlightGroup[rowCounter],
                    StartOfGroupHighlightStrategy, BoldMarkupFormat, EscapeHtml);
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