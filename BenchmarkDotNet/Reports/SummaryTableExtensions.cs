using System.Linq;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Reports
{
    public static class SummaryTableExtensions
    {
        public static void PrintCommonColumns(this SummaryTable table, ILogger logger)
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
            }
        }

        public static void PrintLine(this SummaryTable table, string[] line, ILogger logger, string leftDel = "", string rightDel = "")
        {
            for (int columnIndex = 0; columnIndex < table.ColumnCount; columnIndex++)
                if (table.Columns[columnIndex].NeedToShow)
                    logger.WriteStatistic(leftDel + line[columnIndex].PadLeft(table.Columns[columnIndex].Width, ' ') + rightDel);
        }
    }
}