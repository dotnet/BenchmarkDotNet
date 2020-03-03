using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
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

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            logger.WriteLine("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8' /><head>");
            logger.WriteLine($"<title>Pretty Output of DisassemblyDiagnoser for {summary.Title}</title>");
            logger.WriteLine(InstructionPointerExporter.CssStyle);
            logger.WriteLine(@"
<style type='text/css'>
    td.label:hover { cursor: pointer; background-color: yellow !important; }
    td.highlighted { background-color: yellow !important; }
</style></head><body>");
            logger.WriteLine("<script src=\"https://ajax.aspnetcdn.com/ajax/jQuery/jquery-3.2.1.min.js\"></script>");
            logger.WriteLine($"<script>{HighlightingLabelsScript.Value}</script>");

            int referenceIndex = 0;

            foreach (var benchmarkCase in summary.BenchmarksCases.Where(results.ContainsKey))
            {
                Export(logger, summary, results[benchmarkCase], benchmarkCase, ref referenceIndex);
            }

            logger.WriteLine("</body></html>");
        }

        private void Export(ILogger logger, Summary summary, DisassemblyResult disassemblyResult, BenchmarkCase benchmarkCase, ref int referenceIndex)
        {
            logger.WriteLine($"<h2>{summary[benchmarkCase].GetRuntimeInfo()}</h2>");
            logger.WriteLine("<table><tbody>");

            int methodIndex = 0;
            foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
            {
                referenceIndex++;
                logger.WriteLine($"<tr><th colspan=\"2\" style=\"text-align: left;\">{method.Name}</th><th></th></tr>");

                var pretty = DisassemblyPrettifier.Prettify(method, disassemblyResult, config, $"M{methodIndex++:00}");

                bool even = false, diffTheLabels = pretty.Count > 1;
                foreach (var element in pretty)
                {
                    if (element is DisassemblyPrettifier.Label label)
                    {
                        even = !even;

                        logger.WriteLine($"<tr class=\"{(even && diffTheLabels ? "evenMap" : string.Empty)}\">");
                        logger.WriteLine($"<td id=\"{referenceIndex}_{label.Id}\" class=\"label\" data-label=\"{referenceIndex}_{label.TextRepresentation}\"><pre><code>{label.TextRepresentation}</pre></code></td>");
                        logger.WriteLine("<td>&nbsp;</td></tr>");

                        continue;
                    }

                    logger.WriteLine($"<tr class=\"{(even && diffTheLabels ? "evenMap" : string.Empty)}\">");
                    logger.Write("<td></td>");

                    if (element is DisassemblyPrettifier.Reference reference)
                        logger.WriteLine($"<td id=\"{referenceIndex}\" class=\"reference\" data-reference=\"{referenceIndex}_{reference.Id}\"><a href=\"#{referenceIndex}_{reference.Id}\"><pre><code>{reference.TextRepresentation}</pre></code></a></td>");
                    else
                        logger.WriteLine($"<td><pre><code>{element.TextRepresentation}</pre></code></td>");

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

            logger.WriteLine("</tbody></table>");
        }
    }
}