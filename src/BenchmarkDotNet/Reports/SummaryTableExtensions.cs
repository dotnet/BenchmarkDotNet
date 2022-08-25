using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Reports
{
    public static class SummaryTableExtensions
    {
        [ThreadStatic]
        private static StringBuilder sharedBuffer;

        public static void PrintCommonColumns(this SummaryTable table, ILogger logger)
        {
            var commonColumns = table.Columns.Where(c => c.IsCommon).ToArray();
            if (commonColumns.Any())
            {
                int paramsOnLine = 0;
                foreach (var column in commonColumns)
                {
                    logger.WriteInfo($"{column.Header}={column.Content[0]}  ");
                    paramsOnLine++;
                    if (paramsOnLine == 3)
                    {
                        logger.WriteLine();
                        paramsOnLine = 0;
                    }
                }
                if (paramsOnLine != 0)
                    logger.WriteLine();
            }
        }

        public static void PrintLine(this SummaryTable table, string[] line, ILogger logger, string leftDel, string rightDel)
        {
            for (int columnIndex = 0; columnIndex < table.ColumnCount; columnIndex++)
            {
                if (table.Columns[columnIndex].NeedToShow)
                {
                    logger.WriteStatistic(BuildStandardText(table, line, leftDel, rightDel, columnIndex));
                }
            }

            logger.WriteLine();
        }

        public static void PrintLine(this SummaryTable table, string[] line, ILogger logger, string leftDel, string rightDel,
                                     bool highlightRow, bool startOfGroup, MarkdownExporter.MarkdownHighlightStrategy startOfGroupHighlightStrategy, string boldMarkupFormat, bool escapeHtml)
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

                if (highlightRow) // write the row in an alternative color
                {
                    logger.WriteHeader(text);
                }
                else
                {
                    logger.WriteStatistic(text);
                }
            }

            if (startOfGroup && startOfGroupHighlightStrategy == MarkdownExporter.MarkdownHighlightStrategy.Marker)
                logger.Write(highlightRow ? LogKind.Header : LogKind.Statistic, " ^"); //

            logger.WriteLine();
        }

        private static string BuildStandardText(SummaryTable table, string[] line, string leftDel, string rightDel, int columnIndex)
        {
            var buffer = GetClearBuffer();

            buffer.Append(leftDel);
            PadLeft(table, line, leftDel, rightDel, columnIndex, buffer);
            buffer.Append(line[columnIndex]);
            buffer.Append(rightDel);

            return buffer.ToString();
        }

        private static string BuildBoldText(SummaryTable table, string[] line, string leftDel, string rightDel, int columnIndex, string boldMarkupFormat)
        {
            var buffer = GetClearBuffer();

            buffer.Append(leftDel);
            PadLeft(table, line, leftDel, rightDel, columnIndex, buffer);
            buffer.AppendFormat(boldMarkupFormat, line[columnIndex]);
            buffer.Append(rightDel);

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
        private static void PadLeft(SummaryTable table, string[] line, string leftDel, string rightDel, int columnIndex, StringBuilder buffer)
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