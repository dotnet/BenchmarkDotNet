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
    internal class GithubMarkdownDisassemblyExporter : ExporterBase
    {
        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;
        private readonly DisassemblyDiagnoserConfig config;

        internal GithubMarkdownDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results, DisassemblyDiagnoserConfig config)
        {
            this.results = results;
            this.config = config;
        }

        protected override string FileExtension => "md";
        protected override string FileCaption => "asm";

        public override async ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken)
        {
            foreach (var benchmarkCase in summary.BenchmarksCases.Where(results.ContainsKey))
            {
                await writer.WriteLineAsync($"## {summary[benchmarkCase]!.GetRuntimeInfo()} (Job: {benchmarkCase.Job.DisplayInfo})", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);

                await ExportAsync(writer, results[benchmarkCase], config, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        internal static async ValueTask ExportAsync(CancelableStreamWriter writer, DisassemblyResult disassemblyResult, DisassemblyDiagnoserConfig config, bool quotingCode = true, CancellationToken cancellationToken = default)
        {
            int methodIndex = 0;
            foreach (var method in disassemblyResult.Methods.Where(method => method.Problem.IsBlank()))
            {
                if (quotingCode)
                {
                    await writer.WriteLineAsync("```assembly", cancellationToken).ConfigureAwait(false);
                }

                await writer.WriteLineAsync($"; {method.Name}", cancellationToken).ConfigureAwait(false);

                var pretty = DisassemblyPrettifier.Prettify(method, disassemblyResult, config, $"M{methodIndex++:00}");

                ulong totalSizeInBytes = 0;
                foreach (var element in pretty)
                {
                    if (element is DisassemblyPrettifier.Label label)
                    {
                        await writer.WriteLineAsync($"{label.TextRepresentation}:", cancellationToken).ConfigureAwait(false);
                    }
                    else if (element.Source is Sharp sharp)
                    {
                        await writer.WriteLineAsync($"; {sharp.Text.Replace("\n", "\n; ")}", cancellationToken).ConfigureAwait(false); // they are multiline and we need to add ; for each line
                    }
                    else if (element.Source is Asm asm)
                    {
                        checked
                        {
                            totalSizeInBytes += (uint)asm.InstructionLength;
                        }

                        await writer.WriteLineAsync($"       {element.TextRepresentation}", cancellationToken).ConfigureAwait(false);
                    }
                    else if (element.Source is MonoCode mono)
                    {
                        await writer.WriteLineAsync(mono.Text, cancellationToken).ConfigureAwait(false);
                    }
                }

                await writer.WriteLineAsync($"; Total bytes of code {totalSizeInBytes}", cancellationToken).ConfigureAwait(false);
                if (quotingCode)
                {
                    await writer.WriteLineAsync("```", cancellationToken).ConfigureAwait(false);
                }
            }

            foreach (var withProblems in disassemblyResult.Methods
                .Where(method => method.Problem.IsNotBlank())
                .GroupBy(method => method.Problem))
            {
                await writer.WriteLineAsync($"**{withProblems.Key}**", cancellationToken).ConfigureAwait(false);
                foreach (var withProblem in withProblems)
                {
                    await writer.WriteLineAsync(withProblem.Name, cancellationToken).ConfigureAwait(false);
                }
            }

            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}