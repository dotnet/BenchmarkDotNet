using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Reports
{
    public static class SummaryTableExtensions
    {
        [ThreadStatic]
        private static StringBuilder __buffer;

        public static void PrintCommonColumns(this SummaryTable table, ILogger logger)
        {
            var commonColumns = table.Columns.Where(c => !c.NeedToShow && !c.IsDefault).ToArray();
            if (commonColumns.Any())
            {
                var paramsOnLine = 0;
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
                                     bool highlightRow, bool startOfGroup, bool startOfGroupInBold, string boldMarkupFormat)
        {
            for (int columnIndex = 0; columnIndex < table.ColumnCount; columnIndex++)
            {
                if (!table.Columns[columnIndex].NeedToShow)
                {
                    continue;
                }

                var text = (startOfGroup && startOfGroupInBold)
                    ? BuildBoldText(table, line, leftDel, rightDel, columnIndex, boldMarkupFormat)
                    : BuildStandardText(table, line, leftDel, rightDel, columnIndex);

                if (highlightRow) // write the row in an alternative colour
                {
                    logger.WriteHeader(text);
                }
                else
                {
                    logger.WriteStatistic(text);
                }
            }

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
            if (__buffer == null)
            {
                return __buffer = new StringBuilder(28);
            }

            __buffer.Clear();

            return __buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PadLeft(SummaryTable table, string[] line, string leftDel, string rightDel, int columnIndex, StringBuilder buffer)
        {
            const char space = ' ';
            const int extraWidth = 2; // " |".Length is not included in the column's Width

            var repeatCount = table.Columns[columnIndex].Width + extraWidth - leftDel.Length - line[columnIndex].Length - rightDel.Length;
            if (repeatCount > 0)
            {
                buffer.Append(space, repeatCount);
            }
        }
    }
}