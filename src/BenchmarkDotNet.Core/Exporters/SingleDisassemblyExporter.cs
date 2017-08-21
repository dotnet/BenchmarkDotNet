using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    public class SingleDisassemblyExporter : IExporter
    {
        private readonly IReadOnlyDictionary<Benchmark, DisassemblyResult> results;

        public SingleDisassemblyExporter(IReadOnlyDictionary<Benchmark, DisassemblyResult> results)
        {
            this.results = results;
        }

        public string Name => nameof(SingleDisassemblyExporter);

        public void ExportToLog(Summary summary, ILogger logger) { }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
            => summary.Benchmarks
                      .Where(results.ContainsKey)
                      .Select(benchmark => Export(summary, benchmark));

        private string Export(Summary summary, Benchmark benchmark)
        {
            var filePath = $"{Path.Combine(summary.ResultsDirectoryPath, benchmark.FolderInfo)}-asm.html";
            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var stream = Portability.StreamWriter.FromPath(filePath))
            {
                Export(new StreamLogger(stream), results[benchmark], benchmark);
            }

            return filePath;
        }

        private void Export(ILogger logger, DisassemblyResult disassemblyResult, Benchmark benchmark)
        {
            logger.WriteLine("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8' />");
            logger.WriteLine($"<title>Output of DisassemblyDiagnoser for {benchmark.DisplayInfo}</title>");
            logger.WriteLine(InstructionPointerExporter.CssStyle);
            logger.WriteLine("</head>");
            logger.WriteLine("<body>");

            logger.WriteLine("<table>");
            logger.WriteLine("<tbody>");

            var methodNameToNativeCode = disassemblyResult.Methods
                .Where(method => string.IsNullOrEmpty(method.Problem))
                .ToDictionary(method => method.Name, method => method.NativeCode);

            foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
            {
                // I am using NativeCode as the id to avoid any problems with special characters like <> in html ;)
                logger.WriteLine(
                    $"<tr><th colspan=\"2\" id=\"{method.NativeCode}\" style=\"text-align: left;\">{FormatMethodAddress(method.NativeCode)} {method.Name}</th><th></th></tr>");

                // there is no need to distinguish the maps visually if there is only one type of code
                var diffTheMaps = method.Maps.SelectMany(map => map.Instructions).Select(ins => ins.GetType()).Distinct().Count() > 1; 

                bool evenMap = true;
                foreach (var map in method.Maps)
                {
                    foreach (var instruction in map.Instructions)
                    {
                        logger.WriteLine($"<tr class=\"{(evenMap && diffTheMaps ? "evenMap" : string.Empty)}\">");
                        logger.WriteLine($"<td><pre><code>{instruction.TextRepresentation}</pre></code></td>");

                        if (!string.IsNullOrEmpty(instruction.Comment) && methodNameToNativeCode.TryGetValue(instruction.Comment, out var id))
                        {
                            logger.WriteLine($"<td><a href=\"#{id}\">{GetShortName(instruction.Comment)}</a></td>");
                        }
                        else
                        {
                            logger.WriteLine($"<td>{instruction.Comment}</td>");
                        }

                        logger.WriteLine("</tr>");
                    }

                    evenMap = !evenMap;
                }

                logger.WriteLine("<tr><td colspan=\"{2}\">&nbsp;</td></tr>");
            }

            foreach (var withProblems in disassemblyResult.Methods
                    .Where(method => !string.IsNullOrEmpty(method.Problem))
                    .GroupBy(method => method.Problem))
            {
                logger.WriteLine($"<tr><td colspan=\"{2}\"><b>{withProblems.Key}</b></td></tr>");
                foreach (var withProblem in withProblems)
                {
                    logger.WriteLine($"<tr><td colspan=\"{2}\">{withProblem.Name}</td></tr>");
                }
                logger.WriteLine("<tr><td colspan=\"{2}\"></td></tr>");
            }

            logger.WriteLine("</tbody></table></body></html>");
        }

        

        private string GetShortName(string fullMethodSignature)
        {
            var bracketIndex = fullMethodSignature.IndexOf('(');
            var withoutArguments = fullMethodSignature.Remove(bracketIndex);
            var methodNameIndex = withoutArguments.LastIndexOf('.') + 1;

            return withoutArguments.Substring(methodNameIndex);
        }

        // we want to get sth like "00007ffb`a90f4560"
        internal static string FormatMethodAddress(ulong nativeCode)
        {
            if(nativeCode == default(ulong))
                return string.Empty;

            var buffer = new StringBuilder(nativeCode.ToString("x"));

            if (buffer.Length > 8) // 64 bit address
            {
                buffer.Insert(buffer.Length - 8, '`');

                while (buffer.Length < 8 + 1 + 8)
                    buffer.Insert(0, '0');
            }
            else // 32 bit
            {
                while (buffer.Length < 8)
                    buffer.Insert(0, '0');
            }

            return buffer.ToString();
        }
    }
}