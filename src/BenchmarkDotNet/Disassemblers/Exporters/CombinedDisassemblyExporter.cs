using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Disassemblers.Exporters
{
    internal class CombinedDisassemblyExporter : ExporterBase
    {
        internal const string CssDefinition = """
            <style type="text/css">
                table { border-collapse: collapse; display: block; width: 100%; overflow: auto; }
                td, th { padding: 6px 13px; border: 1px solid #ddd; text-align: left; }
                tr { background-color: #fff; border-top: 1px solid #ccc; }
                tr:nth-child(even) { background: #f8f8f8; }
            </style>
            """;

        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;
        private readonly DisassemblyDiagnoserConfig config;

        internal CombinedDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results, DisassemblyDiagnoserConfig config)
        {
            this.results = results;
            this.config = config;
        }

        protected override string FileExtension => "html";
        protected override string FileCaption => "disassembly-report";

        public override async ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken)
        {
            var benchmarksByTarget = summary.BenchmarksCases
                .Where(benchmark => results.ContainsKey(benchmark))
                .GroupBy(benchmark => benchmark.Descriptor.WorkloadMethod)
                .ToArray();

            await writer.WriteLineAsync("<!DOCTYPE html>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<html lang='en'>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<head>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<meta charset='utf-8' />", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync($"<title>DisassemblyDiagnoser Output {summary.Title}</title>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(CssDefinition, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</head>", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("<body>", cancellationToken).ConfigureAwait(false);

            if (benchmarksByTarget.Any(group => group.Count() > 1)) // the user is comparing same method for different JITs
            {
                foreach (var targetingSameMethod in benchmarksByTarget)
                {
                    await PrintTableAsync(
                        targetingSameMethod.ToArray(),
                        writer,
                        targetingSameMethod.First().Descriptor.DisplayInfo,
                        benchmark => summary[benchmark]?.GetRuntimeInfo() ?? "",
                        cancellationToken).ConfigureAwait(false);
                }
            }
            else // different methods, same JIT
            {
                await PrintTableAsync(
                    summary.BenchmarksCases.Where(results.ContainsKey).ToArray(),
                    writer,
                    summary.Title,
                    benchmark => $"{benchmark.Descriptor.WorkloadMethod.Name} {summary[benchmark]?.GetRuntimeInfo()}".TrimEnd(),
                    cancellationToken).ConfigureAwait(false);
            }

            await writer.WriteLineAsync("</body>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</html>", cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask PrintTableAsync(BenchmarkCase[] benchmarksCase, CancelableStreamWriter writer, string title, Func<BenchmarkCase, string> headerTitleProvider, CancellationToken cancellationToken)
        {
            await writer.WriteLineAsync("<table>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<thead>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync($"<tr><th colspan=\"{benchmarksCase.Length}\">{title}</th></tr>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<tr>", cancellationToken).ConfigureAwait(false);
            foreach (var benchmark in benchmarksCase)
            {
                await writer.WriteLineAsync($"<th>{headerTitleProvider(benchmark)}</th>", cancellationToken).ConfigureAwait(false);
            }
            await writer.WriteLineAsync("</tr>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</thead>", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("<tbody>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<tr>", cancellationToken).ConfigureAwait(false);
            foreach (var benchmark in benchmarksCase)
            {
                var disassemblyResult = results[benchmark];
                await writer.WriteLineAsync("<td style=\"vertical-align:top;\"><pre><code>", cancellationToken).ConfigureAwait(false);
                foreach (var method in disassemblyResult.Methods.Where(method => method.Problem.IsBlank()))
                {
                    await writer.WriteLineAsync(method.Name, cancellationToken).ConfigureAwait(false);

                    var formatter = config.GetFormatterWithSymbolSolver(disassemblyResult.AddressToNameMapping);

                    foreach (var map in method.Maps)
                        foreach (var sourceCode in map.SourceCodes)
                            await writer.WriteLineAsync(CodeFormatter.Format(sourceCode, formatter, config.PrintInstructionAddresses, disassemblyResult.PointerSize, disassemblyResult.AddressToNameMapping), cancellationToken).ConfigureAwait(false);

                    await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var withProblems in results[benchmark].Methods
                    .Where(method => method.Problem.IsNotBlank())
                    .GroupBy(method => method.Problem))
                {
                    await writer.WriteLineAsync(withProblems.Key, cancellationToken).ConfigureAwait(false);
                    foreach (var withProblem in withProblems)
                    {
                        await writer.WriteLineAsync(withProblem.Name, cancellationToken).ConfigureAwait(false);
                    }
                    await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                }
                await writer.WriteLineAsync("</code></pre></td>", cancellationToken).ConfigureAwait(false);
            }
            await writer.WriteLineAsync("</tr>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</tbody>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</table>", cancellationToken).ConfigureAwait(false);
        }
    }
}
