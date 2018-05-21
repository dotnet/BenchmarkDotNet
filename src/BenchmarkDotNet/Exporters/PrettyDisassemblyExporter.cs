using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    public class PrettyDisassemblyExporter : IExporter
    {
        private static readonly Lazy<string> HighlightingLabelsScript = new Lazy<string>(() => ResourceHelper.LoadTemplate("highlightingLabelsScript.js"));

        private readonly IReadOnlyDictionary<Benchmark, DisassemblyResult> results;

        public PrettyDisassemblyExporter(IReadOnlyDictionary<Benchmark, DisassemblyResult> results) => this.results = results;

        public string Name => nameof(RawDisassemblyExporter);

        public void ExportToLog(Summary summary, ILogger logger) { }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
            => summary.Benchmarks
                      .Where(results.ContainsKey)
                      .Select(benchmark => Export(summary, benchmark));

        private string Export(Summary summary, Benchmark benchmark)
        {
            var filePath = $"{Path.Combine(summary.ResultsDirectoryPath, benchmark.FolderInfo)}-asm.pretty.html";
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
            logger.WriteLine("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8' /><head>");
            logger.WriteLine($"<title>Pretty Output of DisassemblyDiagnoser for {benchmark.DisplayInfo}</title>");
            logger.WriteLine(InstructionPointerExporter.CssStyle);
            logger.WriteLine(@"
<style type='text/css'>
    td.label:hover { cursor: pointer; background-color: yellow !important; }
    td.highlighted { background-color: yellow !important; }
</style>");
            logger.WriteLine("<script src=\"https://ajax.aspnetcdn.com/ajax/jQuery/jquery-3.2.1.min.js\"></script>");
            logger.WriteLine($"<script>{HighlightingLabelsScript.Value}</script>");
            logger.WriteLine("</head><body><table><tbody>");

            int methodIndex = 0, referenceIndex = 0;
            foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
            {
                logger.WriteLine($"<tr><th colspan=\"2\" style=\"text-align: left;\">{method.Name}</th><th></th></tr>");

                var pretty = DisassemblyPrettifier.Prettify(method, $"M{methodIndex++:00}");

                bool even = false, diffTheLabels = pretty.Count > 1;
                foreach (var element in pretty)
                {
                    if (element is DisassemblyPrettifier.Label label)
                    {
                        even = !even;

                        logger.WriteLine($"<tr class=\"{(even && diffTheLabels ? "evenMap" : string.Empty)}\">");
                        logger.WriteLine($"<td id=\"{label.Id}\" class=\"label\" data-label=\"{label.TextRepresentation}\"><pre><code>{label.TextRepresentation}</pre></code></td>");
                        logger.WriteLine("<td>&nbsp;</td></tr>");
                        
                        continue;
                    }

                    logger.WriteLine($"<tr class=\"{(even && diffTheLabels ? "evenMap" : string.Empty)}\">");
                    logger.Write("<td></td>");

                    var tooltip = element.Source is Asm asm ? $"title=\"{asm.TextRepresentation}\"" : null;

                    if (element is DisassemblyPrettifier.Reference reference)
                        logger.WriteLine($"<td id=\"{referenceIndex++}\" class=\"reference\" data-reference=\"{reference.Id}\" {tooltip}><a href=\"#{reference.Id}\"><pre><code>{reference.TextRepresentation}</pre></code></a></td>");
                    else
                        logger.WriteLine($"<td {tooltip}><pre><code>{element.TextRepresentation}</pre></code></td>");

                    logger.Write("</tr>");
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
    }
}