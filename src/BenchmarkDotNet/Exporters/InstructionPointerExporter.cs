using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Text;

namespace BenchmarkDotNet.Exporters
{
    internal class InstructionPointerExporter : IExporter
    {
        internal const string CssStyle = """
            <style type="text/css">
                pre { margin: 0px; }
                table { border-collapse:collapse; }
                td.perMethod { border-top: 1px black solid; }
                tr.evenMap td { background-color: #F5F5F5; }  
            </style>
            """;

        private readonly IHardwareCountersDiagnoser hardwareCountersDiagnoser;
        private readonly DisassemblyDiagnoser disassemblyDiagnoser;

        internal InstructionPointerExporter(IHardwareCountersDiagnoser hardwareCountersDiagnoser, DisassemblyDiagnoser disassemblyDiagnoser)
        {
            this.hardwareCountersDiagnoser = hardwareCountersDiagnoser;
            this.disassemblyDiagnoser = disassemblyDiagnoser;
        }

        public string Name => nameof(InstructionPointerExporter);

        public async ValueTask ExportAsync(Summary summary, ILogger logger, CancellationToken cancellationToken)
        {
            var hardwareCounters = hardwareCountersDiagnoser.Results;
            var disassembly = disassemblyDiagnoser.Results;

            foreach (var disassemblyResult in disassembly)
            {
                if (hardwareCounters.TryGetValue(disassemblyResult.Key, out var pmcStats))
                    await Export(summary, disassemblyResult.Key, disassemblyResult.Value, pmcStats, logger, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask Export(Summary summary, BenchmarkCase benchmarkCase, DisassemblyResult disassemblyResult, PmcStats pmcStats, ILogger logger, CancellationToken cancellationToken)
        {
            string filePath = Path.Combine(summary.ResultsDirectoryPath,
                $"{FolderNameHelper.ToFolderName(benchmarkCase.Descriptor.Type)}." +
                $"{benchmarkCase.Descriptor.WorkloadMethod.Name}." +
                $"{GetShortRuntimeInfo(summary[benchmarkCase]!.GetRuntimeInfo()!)}.counters.html");

            filePath.DeleteFileIfExists();

            var totals = SumHardwareCountersStatsOfBenchmarkedCode(disassemblyResult, pmcStats);
            var perMethod = SumHardwareCountersPerMethod(disassemblyResult, pmcStats);

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
            using var writer = new CancelableStreamWriter(fileStream);
            await ExportAsync(writer, benchmarkCase, totals, perMethod, pmcStats.Counters.Keys.ToArray(), cancellationToken).ConfigureAwait(false);

            logger.WriteLineInfo($"  {filePath.GetBaseName(Directory.GetCurrentDirectory())}");
        }

        /// <summary>
        /// there might be some hardware counter events not belonging to the benchmarked code (for example CLR or BenchmarkDotNet's Engine)
        /// to calculate the % per IP we need to know the total per benchmark, not per process
        /// </summary>
        private static Dictionary<HardwareCounter, (ulong withoutNoise, ulong total)> SumHardwareCountersStatsOfBenchmarkedCode(
            DisassemblyResult disassemblyResult, PmcStats pmcStats)
        {
            IEnumerable<ulong> Range(Asm asm)
            {
                // most probably asm.StartAddress would be enough, but I don't want to miss any edge case
                for (ulong instructionPointer = asm.InstructionPointer; instructionPointer < asm.InstructionPointer + (ulong)asm.InstructionLength; instructionPointer++)
                    yield return instructionPointer;
            }

            var instructionPointers = new HashSet<ulong>(
                disassemblyResult
                    .Methods
                    .SelectMany(method => method.Maps)
                    .SelectMany(map => map.SourceCodes)
                    .OfType<Asm>()
                    .SelectMany(Range)
                    .Distinct());

            return pmcStats.Counters.ToDictionary(data => data.Key, data =>
            {
                ulong withoutNoise = 0, total = 0;

                foreach (var ipToCount in data.Value.PerInstructionPointer)
                {
                    total += ipToCount.Value;

                    if (instructionPointers.Contains(ipToCount.Key))
                        withoutNoise += ipToCount.Value;
                }

                return (withoutNoise, total);
            });
        }

        private static IReadOnlyList<MethodWithCounters> SumHardwareCountersPerMethod(DisassemblyResult disassemblyResult, PmcStats pmcStats)
        {
            var model = new List<MethodWithCounters>(disassemblyResult.Methods.Length);

            foreach (var method in disassemblyResult.Methods.Where(method => method.Problem.IsBlank()))
            {
                var groups = new List<List<CodeWithCounters>>();

                foreach (var map in method.Maps)
                {
                    var codeWithCounters = new List<CodeWithCounters>(map.SourceCodes.Length);

                    foreach (var instruction in map.SourceCodes)
                    {
                        var totalsPerCounter = pmcStats.Counters.Keys.ToDictionary(key => key, _ => default(ulong));

                        if (instruction is Asm asm)
                        {
                            foreach (var hardwareCounter in pmcStats.Counters)
                            {
                                // most probably asm.StartAddress would be enough, but I don't want to miss any edge case
                                for (ulong instructionPointer = asm.InstructionPointer; instructionPointer < asm.InstructionPointer + (ulong)asm.InstructionLength; instructionPointer++)
                                    if (hardwareCounter.Value.PerInstructionPointer.TryGetValue(instructionPointer, out ulong value))
                                        totalsPerCounter[hardwareCounter.Key] = totalsPerCounter[hardwareCounter.Key] + value;
                            }
                        }

                        codeWithCounters.Add(new CodeWithCounters
                        {
                            Code = instruction,
                            SumPerCounter = totalsPerCounter
                        });
                    }

                    groups.Add(codeWithCounters);
                }

                model.Add(new MethodWithCounters
                {
                    Method = method,
                    Instructions = groups,
                    SumPerCounter = pmcStats.Counters.Keys.ToDictionary(
                        hardwareCounter => hardwareCounter,
                        hardwareCounter =>
                        {
                            ulong sum = 0;

                            foreach (var group in groups)
                                foreach (var codeWithCounter in group)
                                    sum += codeWithCounter.SumPerCounter[hardwareCounter];

                            return sum;
                        }
                    )
                });
            }

            return model;
        }

        private async ValueTask ExportAsync(
            CancelableStreamWriter writer,
            BenchmarkCase benchmarkCase,
            Dictionary<HardwareCounter, (ulong withoutNoise, ulong total)> totals,
            IReadOnlyList<MethodWithCounters> model,
            HardwareCounter[] hardwareCounters,
            CancellationToken cancellationToken)
        {
            int columnsCount = hardwareCounters.Length + 1;

            await writer.WriteLineAsync("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8' />", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync($"<title>Combined Output of DisassemblyDiagnoser and HardwareCounters {benchmarkCase.DisplayInfo}</title>", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(CssStyle, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("</head>", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("<body>", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("<!-- Generated with BenchmarkDotNet ", cancellationToken).ConfigureAwait(false);
            foreach (var total in totals)
            {
                // this stats are mostly for me, the maintainer, who wants to know if removing noise makes any sense
                await writer.WriteLineAsync($"For {total.Key} we have {total.Value.total} in total, {total.Value.withoutNoise} without noise", cancellationToken).ConfigureAwait(false);
            }
            await writer.WriteLineAsync("-->", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("<table><thead><tr>", cancellationToken).ConfigureAwait(false);

            foreach (var hardwareCounter in hardwareCounters)
                await writer.WriteLineAsync($"<th>{hardwareCounter.ToShortName()}</th>", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("<th></th></tr></thead>", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("<tbody>", cancellationToken).ConfigureAwait(false);
            var disassemblyResult = disassemblyDiagnoser.Results[benchmarkCase];
            var config = disassemblyDiagnoser.Config;
            var formatterWithSymbols = config.GetFormatterWithSymbolSolver(disassemblyResult.AddressToNameMapping);
            foreach (var method in model.Where(data => data.HasCounters))
            {
                await writer.WriteLineAsync($"<tr><th colspan=\"{columnsCount}\">{method.Method.Name}</th></tr>", cancellationToken).ConfigureAwait(false);

                bool evenMap = true;
                foreach (var map in method.Instructions)
                {
                    foreach (var instruction in map)
                    {
                        await writer.WriteLineAsync($"<tr class=\"{(evenMap ? "evenMap" : "oddMap")}\">", cancellationToken).ConfigureAwait(false);

                        foreach (var hardwareCounter in hardwareCounters)
                        {
                            ulong totalWithoutNoise = totals[hardwareCounter].withoutNoise;
                            ulong forRange = instruction.SumPerCounter[hardwareCounter];

                            await writer.WriteAsync(forRange != 0
                                ? $"<td title=\"{forRange} of {totalWithoutNoise}\">{(double)forRange / totalWithoutNoise:P}</td>"
                                : "<td>-</td>", cancellationToken).ConfigureAwait(false);
                        }

                        if (instruction.Code is Sharp sharp && sharp.FilePath.IsNotBlank())
                            await writer.WriteAsync($"<td title=\"{sharp.FilePath} line {sharp.LineNumber}\">", cancellationToken).ConfigureAwait(false);
                        else
                            await writer.WriteAsync("<td>", cancellationToken).ConfigureAwait(false);

                        string formatted = CodeFormatter.Format(instruction.Code, formatterWithSymbols, config.PrintInstructionAddresses, disassemblyResult.PointerSize, disassemblyResult.AddressToNameMapping);
                        await writer.WriteLineAsync($"<pre><code>{formatted}</pre></code></td></tr>", cancellationToken).ConfigureAwait(false);
                    }

                    evenMap = !evenMap;
                }

                await writer.WriteLineAsync("<tr>", cancellationToken).ConfigureAwait(false);
                foreach (var hardwareCounter in hardwareCounters)
                {
                    ulong totalWithoutNoise = totals[hardwareCounter].withoutNoise;
                    ulong forMethod = method.SumPerCounter[hardwareCounter];

                    await writer.WriteAsync(forMethod != 0
                        ? $"<td class=\"perMethod\" title=\"{forMethod} of {totalWithoutNoise}\">{(double)forMethod / totalWithoutNoise:P}</td>"
                        : "<td  class=\"perMethod\">-</td>", cancellationToken).ConfigureAwait(false);
                }
                await writer.WriteLineAsync("<td></td></tr>", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync($"<tr><td colspan=\"{columnsCount}\"></td></tr>", cancellationToken).ConfigureAwait(false);
            }

            if (model.Any(data => !data.HasCounters))
            {
                await writer.WriteLineAsync($"<tr><td colspan=\"{columnsCount}\">Method(s) without any hardware counters:</td></tr>", cancellationToken).ConfigureAwait(false);

                foreach (var method in model.Where(data => !data.HasCounters))
                    await writer.WriteLineAsync($"<tr><td colspan=\"{columnsCount}\">{method.Method.Name}</td></tr>", cancellationToken).ConfigureAwait(false);
            }

            await writer.WriteLineAsync("</tbody></table></body></html>", cancellationToken).ConfigureAwait(false);
        }

        // fullInfo is sth like ".NET Core 2.1.21 (CoreCLR 4.6.29130.01, CoreFX 4.6.29130.02), X64 RyuJIT"
        private static string GetShortRuntimeInfo(string fullInfo)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < fullInfo.IndexOf('(') - 1; i++)
            {
                if (fullInfo[i] != ' ')
                {
                    builder.Append(fullInfo[i]);
                }
                else
                {
                    builder.Append('_');
                }
            }

            for (int i = fullInfo.LastIndexOf(',') + 1; i < fullInfo.Length; i++)
            {
                if (fullInfo[i] != ' ')
                {
                    builder.Append(fullInfo[i]);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
        }

        private class CodeWithCounters
        {
            internal required SourceCode Code { get; set; }
            internal required IReadOnlyDictionary<HardwareCounter, ulong> SumPerCounter { get; set; }
        }

        private class MethodWithCounters
        {
            internal required DisassembledMethod Method { get; set; }
            internal required IReadOnlyList<IReadOnlyList<CodeWithCounters>> Instructions { get; set; }
            internal required IReadOnlyDictionary<HardwareCounter, ulong> SumPerCounter { get; set; }

            internal bool HasCounters => SumPerCounter.Values.Any(value => value != default);
        }
    }
}