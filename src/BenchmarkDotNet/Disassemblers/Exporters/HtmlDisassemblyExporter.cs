using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Disassemblers.Exporters
{
    internal class HtmlDisassemblyExporter : ExporterBase
    {
        private static readonly Lazy<string> HighlightingLabelsScript = new Lazy<string>(() => ResourceHelper.LoadTemplate("highlightingLabelsScript.js"));

        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;
        private readonly DisassemblyDiagnoserConfig config;

        internal HtmlDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results, DisassemblyDiagnoserConfig config)
        {
            this.results = results;
            this.config = config;
        }

        protected override string FileExtension => "html";
        protected override string FileCaption => "asm";

        protected override async ValueTask ExportAsync(Summary summary, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
        {
            await writer.WriteLineAsync("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8' /><head>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync($"<title>Pretty Output of DisassemblyDiagnoser for {summary.Title}</title>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(InstructionPointerExporter.CssStyle, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(@"
<style type='text/css'>
    td.label:hover { cursor: pointer; background-color: yellow !important; }
    td.highlighted { background-color: yellow !important; }
</style></head><body>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<script src=\"https://ajax.aspnetcdn.com/ajax/jQuery/jquery-3.2.1.min.js\"></script>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync($"<script>{HighlightingLabelsScript.Value}</script>", cancellationToken).ConfigureAwait(false);

            int referenceIndex = 0;

            foreach (var benchmarkCase in summary.BenchmarksCases.Where(results.ContainsKey))
            {
                referenceIndex = await Export(writer, summary, results[benchmarkCase], benchmarkCase, referenceIndex, cancellationToken).ConfigureAwait(false);
            }

            await writer.WriteLineAsync("</body></html>", cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<int> Export(StreamOrLoggerWriter writer, Summary summary, DisassemblyResult disassemblyResult, BenchmarkCase benchmarkCase, int referenceIndex, CancellationToken cancellationToken)
        {
            await writer.WriteLineAsync($"<h2>{summary[benchmarkCase]!.GetRuntimeInfo()}</h2>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync($"<h3>Job: {benchmarkCase.Job.DisplayInfo}</h3>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("<table><tbody>", cancellationToken).ConfigureAwait(false);

            int methodIndex = 0;
            foreach (var method in disassemblyResult.Methods.Where(method => method.Problem.IsBlank()))
            {
                referenceIndex++;
                await writer.WriteLineAsync($"<tr><th colspan=\"2\" style=\"text-align: left;\">{method.Name}</th><th></th></tr>", cancellationToken).ConfigureAwait(false);

                var pretty = DisassemblyPrettifier.Prettify(method, disassemblyResult, config, $"M{methodIndex++:00}");

                bool even = false, diffTheLabels = pretty.Count > 1;
                foreach (var element in pretty)
                {
                    if (element is DisassemblyPrettifier.Label label)
                    {
                        even = !even;

                        await writer.WriteLineAsync($"<tr class=\"{(even && diffTheLabels ? "evenMap" : string.Empty)}\">", cancellationToken).ConfigureAwait(false);
                        await writer.WriteLineAsync($"<td id=\"{referenceIndex}_{label.Id}\" class=\"label\" data-label=\"{referenceIndex}_{label.TextRepresentation}\"><pre><code>{label.TextRepresentation}</pre></code></td>", cancellationToken).ConfigureAwait(false);
                        await writer.WriteLineAsync("<td>&nbsp;</td></tr>", cancellationToken).ConfigureAwait(false);

                        continue;
                    }

                    await writer.WriteLineAsync($"<tr class=\"{(even && diffTheLabels ? "evenMap" : string.Empty)}\">", cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync("<td></td>", cancellationToken).ConfigureAwait(false);

                    if (element is DisassemblyPrettifier.Reference reference)
                        await writer.WriteLineAsync($"<td id=\"{referenceIndex}\" class=\"reference\" data-reference=\"{referenceIndex}_{reference.Id}\"><a href=\"#{referenceIndex}_{reference.Id}\"><pre><code>{reference.TextRepresentation}</pre></code></a></td>", cancellationToken).ConfigureAwait(false);
                    else
                        await writer.WriteLineAsync($"<td><pre><code>{element.TextRepresentation}</pre></code></td>", cancellationToken).ConfigureAwait(false);

                    await writer.WriteAsync("</tr>", cancellationToken).ConfigureAwait(false);
                }

                await writer.WriteLineAsync("<tr><td colspan=\"{2}\">&nbsp;</td></tr>", cancellationToken).ConfigureAwait(false);
            }

            foreach (var withProblems in disassemblyResult.Methods
                .Where(method => method.Problem.IsNotBlank())
                .GroupBy(method => method.Problem))
            {
                await writer.WriteLineAsync($"<tr><td colspan=\"{2}\"><b>{withProblems.Key}</b></td></tr>", cancellationToken).ConfigureAwait(false);
                foreach (var withProblem in withProblems)
                {
                    await writer.WriteLineAsync($"<tr><td colspan=\"{2}\">{withProblem.Name}</td></tr>", cancellationToken).ConfigureAwait(false);
                }
                await writer.WriteLineAsync("<tr><td colspan=\"{2}\"></td></tr>", cancellationToken).ConfigureAwait(false);
            }

            await writer.WriteLineAsync("</tbody></table>", cancellationToken).ConfigureAwait(false);

            return referenceIndex;
        }
    }
}