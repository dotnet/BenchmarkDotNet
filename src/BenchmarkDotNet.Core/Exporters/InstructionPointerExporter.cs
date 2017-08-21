using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    public class InstructionPointerExporter : IExporter
    {
        internal const string CssStyle = @"
<style type=""text/css"">
    pre { margin: 0px; }
    table { border-collapse:collapse; }
    td.perMethod { border-top: 1px black solid; }
    tr.evenMap td { background-color: #F5F5F5; }  
</style>";

        private readonly IHardwareCountersDiagnoser hardwareCountersDiagnoser;
        private readonly IDisassemblyDiagnoser disassemblyDiagnoser;

        internal InstructionPointerExporter(IHardwareCountersDiagnoser hardwareCountersDiagnoser, IDisassemblyDiagnoser disassemblyDiagnoser)
        {
            this.hardwareCountersDiagnoser = hardwareCountersDiagnoser;
            this.disassemblyDiagnoser = disassemblyDiagnoser;
        }

        public string Name => nameof(InstructionPointerExporter);

        public void ExportToLog(Summary summary, ILogger logger) { }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            var hardwareCounters = hardwareCountersDiagnoser.Results;
            var disassembly = disassemblyDiagnoser.Results;

            foreach (var disassemblyResult in disassembly)
            {
                if (hardwareCounters.TryGetValue(disassemblyResult.Key, out var pmcStats))
                    yield return Export(summary, disassemblyResult.Key, disassemblyResult.Value, pmcStats);
            }
        }

        private string Export(Summary summary, Benchmark benchmark, DisassemblyResult disassemblyResult, PmcStats pmcStats)
        {
            var filePath = $"{Path.Combine(summary.ResultsDirectoryPath, benchmark.Target.Method.Name)}-{benchmark.Job.Env.Jit}-{benchmark.Job.Env.Platform}-instructionPointer.html";
            if (File.Exists(filePath))
                File.Delete(filePath);

            checked
            {
                var totals = SumHardwareCountersStatsOfBenchmarkedCode(disassemblyResult, pmcStats);
                var perMethod = SumHardwareCountersPerMethod(disassemblyResult, pmcStats);

                using (var stream = Portability.StreamWriter.FromPath(filePath))
                {
                    Export(new StreamLogger(stream), benchmark, totals, perMethod, pmcStats.Counters.Keys.ToArray());
                }
            }

            return filePath;
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
                for (ulong instructionPointer = asm.StartAddress; instructionPointer < asm.EndAddress; instructionPointer++)
                    yield return instructionPointer;
            }

            var instructionPointers = new HashSet<ulong>(
                disassemblyResult
                    .Methods
                    .SelectMany(method => method.Maps)
                    .SelectMany(map => map.Instructions)
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

            foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
            {
                var groups = new List<List<CodeWithCounters>>();

                foreach (var map in method.Maps)
                {
                    var codeWithCounters = new List<CodeWithCounters>(map.Instructions.Length);

                    foreach (var instruction in map.Instructions)
                    {
                        var totalsPerCounter = pmcStats.Counters.Keys.ToDictionary(key => key, _ => default(ulong));

                        if (instruction is Asm asm)
                        {
                            foreach (var hardwareCounter in pmcStats.Counters)
                            {
                                // most probably asm.StartAddress would be enough, but I don't want to miss any edge case
                                for (ulong instructionPointer = asm.StartAddress; instructionPointer < asm.EndAddress; instructionPointer++)
                                    if (hardwareCounter.Value.PerInstructionPointer.TryGetValue(instructionPointer, out var value))
                                        totalsPerCounter[hardwareCounter.Key] = totalsPerCounter[hardwareCounter.Key] + value;
                            }
                        }

                        codeWithCounters.Add(new CodeWithCounters
                        {
                            Code = instruction,
                            SumPerCounter = totalsPerCounter,
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

        private void Export(ILogger logger, Benchmark benchmark, Dictionary<HardwareCounter, (ulong withoutNoise, ulong total)> totals, IReadOnlyList<MethodWithCounters> model, HardwareCounter[] hardwareCounters)
        {
            int columnsCount = hardwareCounters.Length + 2;

            logger.WriteLine("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8' />");
            logger.WriteLine($"<title>Combined Output of DisassemblyDiagnoser and HardwareCounters {benchmark.DisplayInfo}</title>");
            logger.WriteLine(CssStyle);
            logger.WriteLine("</head>");

            logger.WriteLine("<body>");

            logger.WriteLine("<!-- Generated with BenchmarkDotNet ");
            foreach(var total in totals)
            {
                // this stats are mostly for me, the maintainer, who wants to know if removing noise makes any sense
                logger.WriteLine($"For {total.Key} we have {total.Value.total} in total, {total.Value.withoutNoise} without noise");
            }
            logger.WriteLine("-->");

            logger.WriteLine("<table><thead><tr>");

            foreach (var hardwareCounter in hardwareCounters)
                logger.WriteLine($"<th>{hardwareCounter.ToShortName()}</th>");

            logger.WriteLine("<th></th><th></th></tr></thead>");

            logger.WriteLine("<tbody>");
            foreach (var method in model.Where(data => data.HasCounters))
            {
                logger.WriteLine($"<tr><th colspan=\"{columnsCount}\">{method.Method.Name}</th></tr>");

                bool evenMap = true;
                foreach (var map in method.Instructions)
                {
                    foreach (var instruction in map)
                    {
                        logger.WriteLine($"<tr class=\"{(evenMap ? "evenMap" : "oddMap")}\">");

                        foreach (var hardwareCounter in hardwareCounters)
                        {
                            var totalWithoutNoise = totals[hardwareCounter].withoutNoise;
                            var forRange = instruction.SumPerCounter[hardwareCounter];

                            if (forRange != 0)
                                logger.Write($"<td title=\"{forRange} of {totalWithoutNoise}\">{((double) forRange / totalWithoutNoise):P}</td>");
                            else
                                logger.Write("<td>-</td>");
                        }

                        if (instruction.Code is Sharp sharp && !string.IsNullOrEmpty(sharp.FilePath))
                            logger.Write($"<td title=\"{sharp.FilePath} line {sharp.LineNumber}\">");
                        else
                            logger.Write("<td>");

                        logger.WriteLine($"<pre><code>{instruction.Code.TextRepresentation}</pre></code></td>");
                        logger.WriteLine($"<td>{instruction.Code.Comment}</td>");
                        logger.WriteLine("</tr>");
                    }

                    evenMap = !evenMap;
                }

                logger.WriteLine("<tr>");
                foreach (var hardwareCounter in hardwareCounters)
                {
                    var totalWithoutNoise = totals[hardwareCounter].withoutNoise;
                    var forMethod = method.SumPerCounter[hardwareCounter];

                    if (forMethod != 0)
                        logger.Write($"<td class=\"perMethod\" title=\"{forMethod} of {totalWithoutNoise}\">{((double)forMethod / totalWithoutNoise):P}</td>");
                    else
                        logger.Write("<td  class=\"perMethod\">-</td>");
                }
                logger.WriteLine("<td></td><td></td></tr>");
                logger.WriteLine($"<tr><td colspan=\"{columnsCount}\"></td></tr>");
            }

            if (model.Any(data => !data.HasCounters))
            {
                logger.WriteLine($"<tr><td colspan=\"{columnsCount}\">Method(s) without any hardware counters:</td></tr>");

                foreach (var method in model.Where(data => !data.HasCounters))
                    logger.WriteLine($"<tr><td colspan=\"{columnsCount}\">{method.Method.Name}</td></tr>");
            }

            logger.WriteLine("</tbody></table></body></html>");
        }

        class CodeWithCounters
        {
            internal Diagnosers.Code Code { get; set; }
            internal IReadOnlyDictionary<HardwareCounter, ulong> SumPerCounter { get; set; }
        }

        class MethodWithCounters
        {
            internal DisassembledMethod Method { get; set; }
            internal IReadOnlyList<IReadOnlyList<CodeWithCounters>> Instructions { get; set; }
            internal IReadOnlyDictionary<HardwareCounter, ulong> SumPerCounter { get; set; }

            internal bool HasCounters => SumPerCounter.Values.Any(value => value != default(ulong));
        }
    }
}