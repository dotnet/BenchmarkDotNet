using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using StreamWriter = BenchmarkDotNet.Portability.StreamWriter;

namespace BenchmarkDotNet.Exporters
{
    public class PrettyGithubMarkdownDiffDisassemblyExporter : PrettyGithubMarkdownDisassemblyExporterBase
    {
        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;

        private static bool canRead;

        protected override string FileExtension => "md";
        protected override string FileCaption => "asm.pretty.diff";

        public PrettyGithubMarkdownDiffDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results)
        {
            this.results = results;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var benchmarksCases = summary.BenchmarksCases
                .Where(results.ContainsKey).ToList();

            logger.WriteLine($"## {summary.Title}");

            for (int i = 0; i < benchmarksCases.Count; i++)
            {
                var firstBenchmarkCase = benchmarksCases[i];
                for (int j = i + 1; j < benchmarksCases.Count; j++)
                {
                    var secondBenchmarkCase = benchmarksCases[j];

                    var firstFileName = Export(summary, results[firstBenchmarkCase]);
                    var secondFileName = Export(summary, results[secondBenchmarkCase]);
                    try
                    {
                        var builder = new StringBuilder();

                        RunGitDiff(firstFileName, secondFileName, builder);

                        if (firstBenchmarkCase.Descriptor.WorkloadMethod == secondBenchmarkCase.Descriptor.WorkloadMethod
                        ) // diff between the same method for different JITs
                        {
                            logger.WriteLine($"**Diff for {firstBenchmarkCase.Descriptor.WorkloadMethod.Name} method between:**");
                            logger.WriteLine($"{GetImportantInfo(summary[firstBenchmarkCase])}");
                            logger.WriteLine($"{GetImportantInfo(summary[secondBenchmarkCase])}");
                        }
                        else // different methods, same JIT
                        {
                            logger.WriteLine(
                                $"**Diff between {firstBenchmarkCase.Descriptor.WorkloadMethod.Name} and {secondBenchmarkCase.Descriptor.WorkloadMethod.Name}**");
                            logger.WriteLine($"on {GetImportantInfo(summary[firstBenchmarkCase])}.");
                        }

                        logger.WriteLine("```diff");
                        logger.WriteLine(builder.ToString());
                        logger.WriteLine("```");
                    }
                    finally
                    {
                        File.Delete(firstFileName);
                        File.Delete(secondFileName);
                    }
                }
            }
        }

        private static string Export(Summary summary, DisassemblyResult disassemblyResult)
        {
            string filePath = $"{Path.Combine(summary.ResultsDirectoryPath, Guid.NewGuid().ToString())}-diff.temp";

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var stream = StreamWriter.FromPath(filePath))
            {
                Export(new StreamLogger(stream), disassemblyResult, quotingCode: false);
            }

            return filePath;
        }

        private static string GetImportantInfo(BenchmarkReport benchmarkReport) => benchmarkReport.GetRuntimeInfo();

        private static void RunGitDiff(string firstFile, string secondFile, StringBuilder result)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"diff --no-index --no-color --text {firstFile} {secondFile}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            canRead = false;

            try
            {
                using (var testProcess = Process.Start(startInfo))
                {
                    testProcess.OutputDataReceived += (s, e) => ProcessOutput(e.Data, result);
                    testProcess.BeginOutputReadLine();

                    testProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                result.AppendLine("An exception occurred during run Git. Please check if you have Git installed on your system and Git is added to PATH.");
                result.AppendLine($"Exception: {ex.Message}");
            }
        }

        private static void ProcessOutput(string line, StringBuilder result)
        {
            if (!string.IsNullOrEmpty(line) && line.Contains("@@"))
            {
                canRead = true;
                return;
            }

            if (canRead)
            {
                result.AppendLine(line);
            }
        }
    }
}