using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Disassemblers.Exporters
{
    internal class GithubMarkdownDiffDisassemblyExporter : ExporterBase
    {
        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;
        private readonly DisassemblyDiagnoserConfig config;

        protected override string FileExtension => "md";
        protected override string FileCaption => "asm.pretty.diff";

        internal GithubMarkdownDiffDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results, DisassemblyDiagnoserConfig config)
        {
            this.results = results;
            this.config = config;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var benchmarksCases = summary.BenchmarksCases.Where(results.ContainsKey).ToArray();

            logger.WriteLine($"## {summary.Title}");
            for (int i = 0; i < benchmarksCases.Length; i++)
            {
                var firstBenchmarkCase = benchmarksCases[i];
                for (int j = i + 1; j < benchmarksCases.Length; j++)
                {
                    var secondBenchmarkCase = benchmarksCases[j];

                    ExportDiff(summary, logger, firstBenchmarkCase, secondBenchmarkCase);
                }
            }
        }

        private void ExportDiff(Summary summary, ILogger logger, BenchmarkCase firstBenchmarkCase, BenchmarkCase secondBenchmarkCase)
        {
            // We want to get diff for the same method and different JITs
            if (firstBenchmarkCase.Descriptor.WorkloadMethod == secondBenchmarkCase.Descriptor.WorkloadMethod)
            {
                var firstFileName = SaveDisassemblyResult(summary, results[firstBenchmarkCase]);
                var secondFileName = SaveDisassemblyResult(summary, results[secondBenchmarkCase]);
                try
                {
                    var builder = new StringBuilder();

                    RunGitDiff(firstFileName, secondFileName, builder);

                    logger.WriteLine($"**Diff for {firstBenchmarkCase.Descriptor.WorkloadMethod.Name} method between:**");
                    logger.WriteLine($"{GetImportantInfo(summary[firstBenchmarkCase])}");
                    logger.WriteLine($"{GetImportantInfo(summary[secondBenchmarkCase])}");

                    logger.WriteLine("```diff");
                    logger.WriteLine(builder.ToString().Trim());
                    logger.WriteLine("```");
                }
                finally
                {
                    File.Delete(firstFileName);
                    File.Delete(secondFileName);
                }
            }
        }

        private string SaveDisassemblyResult(Summary summary, DisassemblyResult disassemblyResult)
        {
            string filePath = $"{Path.Combine(summary.ResultsDirectoryPath, Guid.NewGuid().ToString())}-diff.temp";

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var stream = new StreamWriter(filePath, append: false))
            {
                using (var streamLogger = new StreamLogger(stream))
                {
                    GithubMarkdownDisassemblyExporter.Export(streamLogger, disassemblyResult, config, quotingCode: false);
                }
            }

            return filePath;
        }

        private static string GetImportantInfo(BenchmarkReport benchmarkReport) => benchmarkReport.GetRuntimeInfo();

        private static void RunGitDiff(string firstFile, string secondFile, StringBuilder result)
        {
            try
            {
                (int exitCode, IReadOnlyList<string> output) = ProcessHelper.RunAndReadOutputLineByLine("git", $"diff --no-index --no-color --text --function-context {firstFile} {secondFile}");

                bool canRead = false;

                foreach (string line in output)
                {
                    if (!string.IsNullOrEmpty(line) && line.Contains("@@"))
                    {
                        canRead = true;
                        continue;
                    }

                    if (canRead)
                    {
                        result.AppendLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                result.AppendLine("An exception occurred during run Git. Please check if you have Git installed on your system and Git is added to PATH.");
                result.AppendLine($"Exception: {ex.Message}");
            }
        }
    }
}