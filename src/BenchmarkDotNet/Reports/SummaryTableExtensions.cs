using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using System.Runtime.CompilerServices;
using System.Text;

namespace BenchmarkDotNet.Reports
{
    internal static class SummaryTableExtensions
    {
        [ThreadStatic]
        private static StringBuilder? sharedBuffer;

        public static async ValueTask PrintCommonColumnsAsync(this SummaryTable table, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
        {
            var commonColumns = table.Columns.Where(c => c.IsCommon).ToArray();
            if (commonColumns.Any())
            {
                int paramsOnLine = 0;
                foreach (var column in commonColumns)
                {
                    await writer.WriteAsync($"{column.Header}={column.Content[0]}  ", LogKind.Info, cancellationToken).ConfigureAwait(false);
                    paramsOnLine++;
                    if (paramsOnLine == 3)
                    {
                        await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                        paramsOnLine = 0;
                    }
                }
                if (paramsOnLine != 0)
                    await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public static async ValueTask PrintLineAsync(this SummaryTable table, string[] line, StreamOrLoggerWriter writer, string leftDel, string rightDel, CancellationToken cancellationToken)
        {
            for (int columnIndex = 0; columnIndex < table.ColumnCount; columnIndex++)
            {
                if (table.Columns[columnIndex].NeedToShow)
                {
                    await writer.WriteAsync(BuildStandardText(table, line, leftDel, rightDel, columnIndex), LogKind.Statistic, cancellationToken).ConfigureAwait(false);
                }
            }

            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async ValueTask PrintLineAsync(this SummaryTable table, string[] line, StreamOrLoggerWriter writer, string leftDel, string rightDel,
            bool highlightRow, bool startOfGroup, MarkdownExporter.MarkdownHighlightStrategy startOfGroupHighlightStrategy, string boldMarkupFormat,
            bool escapeHtml, CancellationToken cancellationToken)
        {
            for (int columnIndex = 0; columnIndex < table.ColumnCount; columnIndex++)
            {
                if (!table.Columns[columnIndex].NeedToShow)
                {
                    continue;
                }

                string text = startOfGroup && startOfGroupHighlightStrategy == MarkdownExporter.MarkdownHighlightStrategy.Bold
                    ? BuildBoldText(table, line, leftDel, rightDel, columnIndex, boldMarkupFormat)
                    : BuildStandardText(table, line, leftDel, rightDel, columnIndex);
                if (escapeHtml)
                    text = text.HtmlEncode();

                // write the row in an alternative color
                await writer.WriteAsync(text, highlightRow ? LogKind.Header : LogKind.Statistic, cancellationToken).ConfigureAwait(false);
            }

            if (startOfGroup && startOfGroupHighlightStrategy == MarkdownExporter.MarkdownHighlightStrategy.Marker)
                await writer.WriteAsync(" ^", highlightRow ? LogKind.Header : LogKind.Statistic, cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
        }

        private static string BuildStandardText(SummaryTable table, string[] line, string leftDel, string rightDel, int columnIndex)
        {
            var buffer = GetClearBuffer();
            var isBuildingHeader = table.FullHeader[columnIndex] == line[columnIndex];
            var columnJustification = isBuildingHeader ? SummaryTable.SummaryTableColumn.TextJustification.Left : table.Columns[columnIndex].Justify;

            buffer.Append(leftDel);
            if (columnJustification == SummaryTable.SummaryTableColumn.TextJustification.Right)
            {
                AddPadding(table, line, leftDel, rightDel, columnIndex, buffer);
            }

            buffer.Append(line[columnIndex]);

            if (columnJustification == SummaryTable.SummaryTableColumn.TextJustification.Left)
            {
                AddPadding(table, line, leftDel, rightDel, columnIndex, buffer);
            }
            var isLastColumn = columnIndex == table.ColumnCount - 1;
            buffer.Append(isLastColumn ? rightDel.TrimEnd() : rightDel);

            return buffer.ToString();
        }

        private static string BuildBoldText(SummaryTable table, string[] line, string leftDel, string rightDel, int columnIndex, string boldMarkupFormat)
        {
            var buffer = GetClearBuffer();
            var isBuildingHeader = table.FullHeader[columnIndex] == line[columnIndex];
            var columnJustification = isBuildingHeader ? SummaryTable.SummaryTableColumn.TextJustification.Left : table.Columns[columnIndex].Justify;

            buffer.Append(leftDel);
            if (columnJustification == SummaryTable.SummaryTableColumn.TextJustification.Right)
            {
                AddPadding(table, line, leftDel, rightDel, columnIndex, buffer);
            }

            buffer.AppendFormat(boldMarkupFormat, line[columnIndex]);

            if (columnJustification == SummaryTable.SummaryTableColumn.TextJustification.Left)
            {
                AddPadding(table, line, leftDel, rightDel, columnIndex, buffer);
            }
            var isLastColumn = columnIndex == table.ColumnCount - 1;
            buffer.Append(isLastColumn ? rightDel.TrimEnd() : rightDel);

            return buffer.ToString();
        }

        private static StringBuilder GetClearBuffer()
        {
            if (sharedBuffer == null)
            {
                return sharedBuffer = new StringBuilder(28);
            }

            sharedBuffer.Clear();

            return sharedBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddPadding(SummaryTable table, string[] line, string leftDel, string rightDel, int columnIndex, StringBuilder buffer)
        {
            const char space = ' ';
            const int extraWidth = 2; // " |".Length is not included in the column's Width

            int repeatCount = table.Columns[columnIndex].Width + extraWidth - leftDel.Length - line[columnIndex].Length - rightDel.Length;
            if (repeatCount > 0)
            {
                buffer.Append(space, repeatCount);
            }
        }
    }
}