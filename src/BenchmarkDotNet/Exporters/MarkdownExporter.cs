using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Helpers;
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

        protected string Dialect { get; set; } = default!;

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
            CodeBlockStart = "```",
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
        [PublicAPI] protected string TableHeaderSeparator = " | ";
        [PublicAPI] protected string TableColumnSeparator = " | ";
        [PublicAPI] protected bool UseHeaderSeparatingRow = true;
        [PublicAPI] protected bool ColumnsStartWithSeparator;
        [PublicAPI] protected string BoldMarkupFormat = "**{0}**";
        [PublicAPI] protected bool EscapeHtml;

        protected MarkdownExporter() { }

        public override ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken)
            => ExportCore(summary, new StreamWriterWrapper(writer), cancellationToken);

        internal ValueTask ExportToLogAsync(Summary summary, ILogger logger, CancellationToken cancellationToken)
            => ExportCore(summary, new LoggerWriter(logger), cancellationToken);

        private async ValueTask ExportCore(Summary summary, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
        {
            if (UseCodeBlocks)
            {
                await writer.WriteLineAsync(CodeBlockStart, cancellationToken).ConfigureAwait(false);
            }

            var prefixedWriter = GetPrefixedWriter(writer);

            await prefixedWriter.WriteLineAsync(cancellationToken).ConfigureAwait(false);
            foreach (string infoLine in summary.HostEnvironmentInfo.ToFormattedString())
            {
                await prefixedWriter.WriteLineAsync(infoLine, LogKind.Info, cancellationToken).ConfigureAwait(false);
            }

            await prefixedWriter.WriteLineAsync(summary.AllRuntimes, LogKind.Info, cancellationToken).ConfigureAwait(false);
            await prefixedWriter.WriteLineAsync(cancellationToken).ConfigureAwait(false);

            await PrintTableAsync(summary.Table, prefixedWriter, cancellationToken).ConfigureAwait(false);

            // TODO: move this logic to an analyzer
            var benchmarksWithTroubles = summary.Reports.Where(r => !r.GetResultRuns().Any()).Select(r => r.BenchmarkCase).ToList();
            if (benchmarksWithTroubles.Count > 0)
            {
                await prefixedWriter.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                await prefixedWriter.WriteLineAsync("Benchmarks with issues:", LogKind.Error, cancellationToken).ConfigureAwait(false);
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                {
                    await prefixedWriter.WriteLineAsync("  " + benchmarkWithTroubles.DisplayInfo, LogKind.Error, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private StreamOrLoggerWriter GetPrefixedWriter(StreamOrLoggerWriter writer)
        {
            if (string.IsNullOrEmpty(Prefix))
                return writer;
            return new PrefixedStreamOrLoggerWriter(writer, Prefix);
        }

        private async ValueTask PrintTableAsync(SummaryTable table, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
        {
            if (table.FullContent.Length == 0)
            {
                await writer.WriteLineAsync("There are no benchmarks found ", LogKind.Error, cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await table.PrintCommonColumnsAsync(writer, cancellationToken).ConfigureAwait(false);

            if (table.Columns.All(c => !c.NeedToShow))
            {
                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("There are no columns to show ", cancellationToken).ConfigureAwait(false);
                return;
            }

            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);

            if (UseCodeBlocks)
            {
                await writer.WriteAsync(CodeBlockEnd, cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
            }

            await writer.WriteAsync(ColumnsStartWithSeparator ? TableHeaderSeparator.TrimStart() : " ", LogKind.Statistic, cancellationToken).ConfigureAwait(false);

            await table.PrintLineAsync(table.FullHeader, writer, string.Empty, TableHeaderSeparator, cancellationToken).ConfigureAwait(false);
            if (UseHeaderSeparatingRow)
            {
                await writer.WriteAsync(ColumnsStartWithSeparator ? TableHeaderSeparator.TrimStart().TrimEnd() + "-" : "-", LogKind.Statistic, cancellationToken).ConfigureAwait(false);

                await writer.WriteLineAsync(string.Join("",
                    table.Columns.Where(c => c.NeedToShow).Select((column, index) =>
                        new string('-', column.Width - 1) + GetHeaderSeparatorIndicator(column.OriginalColumn.IsNumeric) +
                        GetHeaderSeparatorColumnDivider(index, table.Columns.Where(c => c.NeedToShow).Count()))), LogKind.Statistic, cancellationToken).ConfigureAwait(false);
            }

            int rowCounter = 0;
            bool highlightRow = false;
            var separatorLine = Enumerable.Range(0, table.ColumnCount).Select(_ => "").ToArray();
            foreach (var line in table.FullContent)
            {
                if (rowCounter > 0 && table.FullContentStartOfLogicalGroup[rowCounter] && table.SeparateLogicalGroups)
                {
                    // Print logical separator
                    await writer.WriteAsync(ColumnsStartWithSeparator ? TableColumnSeparator.TrimStart() : " ", LogKind.Statistic, cancellationToken).ConfigureAwait(false);

                    await table.PrintLineAsync(separatorLine, writer, string.Empty, TableColumnSeparator, highlightRow, false, StartOfGroupHighlightStrategy,
                        BoldMarkupFormat, false, cancellationToken).ConfigureAwait(false);
                }

                // Each time we hit the start of a new group, alternative the color (in the console) or display bold in Markdown
                if (table.FullContentStartOfHighlightGroup[rowCounter])
                {
                    highlightRow = !highlightRow;
                }

                await writer.WriteAsync(ColumnsStartWithSeparator ? TableColumnSeparator.TrimStart() : " ", LogKind.Statistic, cancellationToken).ConfigureAwait(false);

                await table.PrintLineAsync(line, writer, string.Empty, TableColumnSeparator, highlightRow, table.FullContentStartOfHighlightGroup[rowCounter],
                    StartOfGroupHighlightStrategy, BoldMarkupFormat, EscapeHtml, cancellationToken).ConfigureAwait(false);
                rowCounter++;
            }
        }

        private static string GetHeaderSeparatorIndicator(bool isNumeric)
        {
            return isNumeric ? ":" : " ";
        }

        private static string GetHeaderSeparatorColumnDivider(int columnIndex, int columnCount)
        {
            var isLastColumn = columnIndex != columnCount - 1;
            return isLastColumn ? "|-" : "|";
        }
    }
}
