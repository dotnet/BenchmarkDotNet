using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Export
{
    public class MarkdownReportExporter : IReportExporter
    {
        public static readonly MarkdownReportExporter Default = new MarkdownReportExporter();

        private MarkdownReportExporter()
        {
        }

        public void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger)
        {
            logger.WriteLineInfo(EnvironmentHelper.GetFullEnvironmentInfo("Host", false));

            var table = ReportExporterHelper.BuildTable(reports);
            // If we have Benchmarks with ParametersSets, force the "Method" columns to be displayed, otherwise it doesn't make as much sense
            var columnsToAlwaysShow = reports.Any(r => r.Benchmark.Task.ParametersSets != null) ? new[] { "Method" } : new string[0];
            PrintTable(table, logger, columnsToAlwaysShow);
            var benchmarksWithTroubles = reports.Where(r => r.Runs.Count == 0).Select(r => r.Benchmark).ToList();
            if (benchmarksWithTroubles.Count > 0)
            {
                logger.NewLine();
                logger.WriteLineError("Benchmarks with troubles:");
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                    logger.WriteLineError("  " + benchmarkWithTroubles.Caption);
            }
        }

        private void PrintTable(List<string[]> table, IBenchmarkLogger logger, string [] columnsToAlwaysShow)
        {
            if (table.Count == 0)
            {
                logger.WriteLineError("There are no found benchmarks");
                logger.NewLine();
                return;
            }
            int rowCount = table.Count, colCount = table[0].Length;
            var columnsToShowIndexes = columnsToAlwaysShow.Select(col => Array.IndexOf(table[0], col));
            int[] widths = new int[colCount];
            bool[] areSame = new bool[colCount];
            for (int colIndex = 0; colIndex < colCount; colIndex++)
            {
                areSame[colIndex] = rowCount > 2 && colIndex < colCount - 3;
                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    widths[colIndex] = Math.Max(widths[colIndex], table[rowIndex][colIndex].Length + 1);
                    if (rowIndex > 1 && table[rowIndex][colIndex] != table[1][colIndex])
                        areSame[colIndex] = false;
                }
            }
            if (areSame.Any(s => s))
            {
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                    if (areSame[colIndex] && columnsToShowIndexes.Contains(colIndex) == false)
                        logger.WriteInfo($"{table[0][colIndex]}={table[1][colIndex]}  ");
                logger.NewLine();
                logger.WriteLineInfo("```");
            }

            table.Insert(1, widths.Select(w => new string('-', w)).ToArray());
            foreach (var row in table)
            {
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                    if (!areSame[colIndex] || columnsToShowIndexes.Contains(colIndex))
                        logger.WriteStatistic(row[colIndex].PadLeft(widths[colIndex], ' ') + " |");
                logger.NewLine();
            }

            if (areSame.Any(s => s))
            {
                logger.WriteLineInfo("```");
                logger.NewLine();
            }
        }
    }
}