using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Text;

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

        public override async ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken)
        {
            var benchmarksCases = summary.BenchmarksCases.Where(results.ContainsKey).ToArray();

            await writer.WriteLineAsync($"## {summary.Title}", cancellationToken).ConfigureAwait(false);
            for (int i = 0; i < benchmarksCases.Length; i++)
            {
                var firstBenchmarkCase = benchmarksCases[i];
                for (int j = i + 1; j < benchmarksCases.Length; j++)
                {
                    var secondBenchmarkCase = benchmarksCases[j];

                    await ExportDiff(summary, writer, firstBenchmarkCase, secondBenchmarkCase, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async ValueTask ExportDiff(Summary summary, CancelableStreamWriter writer, BenchmarkCase firstBenchmarkCase, BenchmarkCase secondBenchmarkCase, CancellationToken cancellationToken)
        {
            // We want to get diff for the same method and different JITs
            if (firstBenchmarkCase.Descriptor.WorkloadMethod == secondBenchmarkCase.Descriptor.WorkloadMethod)
            {
                var firstFileName = await SaveDisassemblyResult(summary, results[firstBenchmarkCase], cancellationToken).ConfigureAwait(false);
                var secondFileName = await SaveDisassemblyResult(summary, results[secondBenchmarkCase], cancellationToken).ConfigureAwait(false);
                try
                {
                    var builder = new StringBuilder();

                    await RunGitDiff(firstFileName, secondFileName, builder, cancellationToken).ConfigureAwait(false);

                    await writer.WriteLineAsync($"**Diff for {firstBenchmarkCase.Descriptor.WorkloadMethod.Name} method between:**", cancellationToken).ConfigureAwait(false);
                    await writer.WriteLineAsync($"{GetImportantInfo(summary[firstBenchmarkCase]!)}", cancellationToken).ConfigureAwait(false);
                    await writer.WriteLineAsync($"{GetImportantInfo(summary[secondBenchmarkCase]!)}", cancellationToken).ConfigureAwait(false);

                    await writer.WriteLineAsync("```diff", cancellationToken).ConfigureAwait(false);
                    await writer.WriteLineAsync(builder.ToString().Trim(), cancellationToken).ConfigureAwait(false);
                    await writer.WriteLineAsync("```", cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    File.Delete(firstFileName);
                    File.Delete(secondFileName);
                }
            }
        }

        private async ValueTask<string> SaveDisassemblyResult(Summary summary, DisassemblyResult disassemblyResult, CancellationToken cancellationToken)
        {
            string filePath = $"{Path.Combine(summary.ResultsDirectoryPath, Guid.NewGuid().ToString())}-diff.temp";

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
            using var writer = new CancelableStreamWriter(fileStream);

            await GithubMarkdownDisassemblyExporter.ExportAsync(writer, disassemblyResult, config, quotingCode: false, cancellationToken).ConfigureAwait(false);

            return filePath;
        }

        private static string GetImportantInfo(BenchmarkReport benchmarkReport) =>
            $"{benchmarkReport.GetRuntimeInfo()} (Job: {benchmarkReport.BenchmarkCase.Job.DisplayInfo})";

        private static async ValueTask RunGitDiff(string firstFile, string secondFile, StringBuilder result, CancellationToken cancellationToken)
        {
            try
            {
                (int exitCode, IReadOnlyList<string> output) = await ProcessHelper
                    .RunAndReadOutputLineByLineAsync("git", $"diff --no-index --no-color --text --function-context {firstFile} {secondFile}", cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

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