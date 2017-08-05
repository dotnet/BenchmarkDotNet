using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    public class InstructionPointerExporter : IExporter
    {
        private const string CssStyle = @"
<style type=""text/css"">
	pre { margin: 0px; }
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

            using (var stream = Portability.StreamWriter.FromPath(filePath))
            {
                Export(new StreamLogger(stream), benchmark, disassemblyResult, pmcStats);
            }

            return filePath;
        }

        private void Export(ILogger logger, Benchmark benchmark, DisassemblyResult disassemblyResult, PmcStats pmcStats)
        {
            logger.WriteLine("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8' />");
            logger.WriteLine($"<title>Combined Output of DisassemblyDiagnoser and HardwareCounters {benchmark.DisplayInfo}</title>");
            logger.WriteLine(CssStyle);
            logger.WriteLine("</head>");

            logger.WriteLine("<body><table><thead><tr>");

            foreach (var hardwareCounter in pmcStats.Counters)
                logger.WriteLine($"<th>{hardwareCounter.Key.ToShortName()}</th>");

            logger.WriteLine("<th></th><th></th></tr></thead>");

            int countersCount = pmcStats.Counters.Count, columnsCount = countersCount + 2;

            var totals = SumHardwareCountersStatsOfBenchmarkedCodeOnly(disassemblyResult, pmcStats);

            logger.WriteLine("<tbody>");
            foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
            {
                logger.WriteLine($"<tr><td colspan=\"{columnsCount}\">{method.Name}</th></tr>");

                foreach (var instruction in method.Instructions)
                {
                    logger.WriteLine("<tr>");
                    if (instruction is Asm asm)
                    {
                        foreach (var hardwareCounter in pmcStats.Counters)
                        {
                            var total = totals[hardwareCounter.Key];
                            ulong forRange = 0;

                            // ETW's InstructionPoiner seems to be the EndOffset of ClrMD !!!
                            for (ulong instructionPointer = asm.InstructionPointerTo; instructionPointer > asm.InstructionPointerFrom; instructionPointer--)
                                if (hardwareCounter.Value.PerInstructionPointer.TryGetValue(instructionPointer, out var value))
                                    checked
                                    {
                                        forRange += value;
                                    }

                            if (forRange != 0)
                                logger.Write($"<td title=\"{forRange} of {total}\">{((double)forRange / total):P}</td>");
                            else
                                logger.Write("<td></td>");
                        }
                    }
                    else
                    {
                        for (int i = 0; i < countersCount; i++)
                            logger.Write("<td></td>");
                    }

                    logger.WriteLine($"<td><pre><code>{instruction.TextRepresentation}</pre></code></td><td>{instruction.Comment}</td>");
                    logger.WriteLine("</tr>");
                }
            }

            logger.WriteLine("</tbody></table></body></html>");
        }

        /// <summary>
        /// there might be some hardware counter events not belonging to the benchmarked code
        /// but for example CLR or BenchmarkDotNet's Engine
        /// to calculate the % per IP we need to know the total per benchmark, not per process
        /// </summary>
        private static Dictionary<HardwareCounter, ulong> SumHardwareCountersStatsOfBenchmarkedCodeOnly(DisassemblyResult disassemblyResult, PmcStats pmcStats)
        {
            IEnumerable<ulong> Range(Asm asm)
            {
                for (ulong instructionPointer = asm.InstructionPointerFrom; instructionPointer <= asm.InstructionPointerTo; instructionPointer++)
                    yield return instructionPointer;
            }

            var instructionPointers = new HashSet<ulong>(
                disassemblyResult
                    .Methods.SelectMany(method => method.Instructions)
                    .OfType<Asm>()
                    .SelectMany(Range)
                    .Distinct());

            return pmcStats.Counters.ToDictionary(data => data.Key, data =>
            {
                ulong sum = 0;

                foreach (var ipToCount in data.Value.PerInstructionPointer)
                    if (instructionPointers.Contains(ipToCount.Key))
                        checked
                        {
                            sum += ipToCount.Value;
                        }

                return sum;
            });
        }
    }
}