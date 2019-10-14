using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    public class CombinedDisassemblyExporter : ExporterBase
    {
        internal const string CssDefinition = @"
<style type=""text/css"">
	table { border-collapse: collapse; display: block; width: 100%; overflow: auto; }
	td, th { padding: 6px 13px; border: 1px solid #ddd; text-align: left; }
	tr { background-color: #fff; border-top: 1px solid #ccc; }
	tr:nth-child(even) { background: #f8f8f8; }
</style>";

        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;

        public CombinedDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results)
        {
            this.results = results;
        }

        protected override string FileExtension => "html";
        protected override string FileCaption => "disassembly-report";

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var benchmarksByTarget = summary.BenchmarksCases
                .Where(benchmark => results.ContainsKey(benchmark))
                .GroupBy(benchmark => benchmark.Descriptor.WorkloadMethod)
                .ToList();

            logger.WriteLine("<!DOCTYPE html>");
            logger.WriteLine("<html lang='en'>");
            logger.WriteLine("<head>");
            logger.WriteLine("<meta charset='utf-8' />");
            logger.WriteLine($"<title>DisassemblyDiagnoser Output {summary.Title}</title>");
            logger.WriteLine(CssDefinition);
            logger.WriteLine("</head>");

            logger.WriteLine("<body>");

            if (benchmarksByTarget.Any(group => group.Count() > 1)) // the user is comparing same method for different JITs
            {
                foreach (var targetingSameMethod in benchmarksByTarget)
                {
                    PrintTable(
                        targetingSameMethod.ToArray(),
                        logger,
                        targetingSameMethod.First().Descriptor.DisplayInfo,
                        benchmark => GetImportantInfo(summary[benchmark]));
                }
            }
            else // different methods, same JIT
            {
                PrintTable(
                    summary.BenchmarksCases.Where(benchmark => results.ContainsKey(benchmark)).ToArray(),
                    logger,
                    summary.Title,
                    benchmark => $"{benchmark.Descriptor.WorkloadMethod.Name} {GetImportantInfo(summary[benchmark])}");
            }

            logger.WriteLine("</body>");
            logger.WriteLine("</html>");
        }

        private void PrintTable(BenchmarkCase[] benchmarksCase, ILogger logger, string title, Func<BenchmarkCase, string> headerTitleProvider)
        {
            logger.WriteLine("<table>");
            logger.WriteLine("<thead>");
            logger.WriteLine($"<tr><th colspan=\"{benchmarksCase.Length}\">{title}</th></tr>");
            logger.WriteLine("<tr>");
            foreach (var benchmark in benchmarksCase)
            {
                logger.WriteLine($"<th>{headerTitleProvider(benchmark)}</th>");
            }
            logger.WriteLine("</tr>");
            logger.WriteLine("</thead>");

            logger.WriteLine("<tbody>");
            logger.WriteLine("<tr>");
            foreach (var benchmark in benchmarksCase)
            {
                logger.WriteLine("<td style=\"vertical-align:top;\"><pre><code>");
                foreach (var method in results[benchmark].Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
                {
                    logger.WriteLine($"{RawDisassemblyExporter.FormatMethodAddress(method.NativeCode)} {method.Name}");

                    foreach (var map in method.Maps)
                        foreach (var instruction in map.Instructions)
                            logger.WriteLine(instruction.TextRepresentation);

                    logger.WriteLine();
                }

                foreach (var withProblems in results[benchmark].Methods
                    .Where(method => !string.IsNullOrEmpty(method.Problem))
                    .GroupBy(method => method.Problem))
                {
                    logger.WriteLine(withProblems.Key);
                    foreach (var withProblem in withProblems)
                    {
                        logger.WriteLine(withProblem.Name);
                    }
                    logger.WriteLine();
                }
                logger.WriteLine("</code></pre></td>");
            }
            logger.WriteLine("</tr>");
            logger.WriteLine("</tbody>");
            logger.WriteLine("</table>");
        }

        private static string GetImportantInfo(BenchmarkReport benchmarkReport) => benchmarkReport.GetRuntimeInfo();
    }
}