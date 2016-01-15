using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    // TODO: add support of GitHub markdown, Stackoverflow markdown
    public class BenchmarkMarkdownExporter : BenchmarkExporterBase
    {
        public override string Name => $"md-{Dialect}";
        public override string Description => $"Markdown exporter - {Dialect}";

        public override string FileExtension => "md";
        public override string FileNameSuffix => $"-{Dialect.ToLower()}";

        public string Dialect { get; private set; }

        public static readonly IBenchmarkExporter Default = new BenchmarkMarkdownExporter()
        {
            Dialect = nameof(Default)
        };
        public static readonly IBenchmarkExporter StackOverflow = new BenchmarkMarkdownExporter()
        {
            prefix = "    ",
            Dialect = nameof(StackOverflow)
        };
        public static readonly IBenchmarkExporter GitHub = new BenchmarkMarkdownExporter()
        {
            Dialect = nameof(GitHub),
            useCodeBlocks = true,
            codeBlocksSyntax = "ini"
        };

        private string prefix = string.Empty;
        private bool useCodeBlocks = false;
        private string codeBlocksSyntax = string.Empty;

        private BenchmarkMarkdownExporter()
        {
        }

        public override void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger)
        {
            if(useCodeBlocks)
                logger.WriteLine($"```{codeBlocksSyntax}");
            logger = new BenchmarkLoggerWithPrefix(logger, prefix);
            logger.WriteLineInfo(EnvironmentInfo.GetCurrentInfo().ToFormattedString("Host"));
            logger.NewLine();

            var table = BenchmarkExporterHelper.BuildTable(reports);
            // If we have Benchmarks with ParametersSets, force the "Method" columns to be displayed, otherwise it doesn't make as much sense
            var columnsToAlwaysShow = reports.Any(r => r.Benchmark.Task.ParametersSets != null) ? new[] { "Method" } : new string[0];
            PrintTable(table, logger, columnsToAlwaysShow);

            // TODO: move this logic to an analyser
            var benchmarksWithTroubles = reports.Where(r => !r.GetTargetRuns().Any()).Select(r => r.Benchmark).ToList();
            if (benchmarksWithTroubles.Count > 0)
            {
                logger.NewLine();
                logger.WriteLineError("Benchmarks with troubles:");
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                    logger.WriteLineError("  " + benchmarkWithTroubles.Caption);
            }
        }

        private void PrintTable(List<string[]> table, IBenchmarkLogger logger, string[] columnsToAlwaysShow)
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
                areSame[colIndex] = rowCount > 2 && colIndex < colCount - 2;
                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    widths[colIndex] = Math.Max(widths[colIndex], table[rowIndex][colIndex].Length + 1);
                    if (rowIndex > 1 && table[rowIndex][colIndex] != table[1][colIndex])
                        areSame[colIndex] = false;
                }
            }
            if (areSame.Any(s => s))
            {
                var paramsOnLine = 0;
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                    if (areSame[colIndex] && columnsToShowIndexes.Contains(colIndex) == false)
                    {
                        logger.WriteInfo($"{table[0][colIndex]}={table[1][colIndex]}  ");
                        paramsOnLine++;
                        if (paramsOnLine == 3)
                        {
                            logger.NewLine();
                            paramsOnLine = 0;
                        }
                    }
                        
                logger.NewLine();
            }

            if (useCodeBlocks)
            {
                logger.Write("```");
                logger.NewLine();
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
                logger.NewLine();
            }
        }
    }
}