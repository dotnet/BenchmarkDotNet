using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Disassemblers.Exporters
{
    internal class CombinedDisassemblyExporter : ExporterBase
    {
        internal const string CssDefinition = @"
<style type=""text/css"">
    table { border-collapse: collapse; display: block; width: 100%; overflow: auto; }
    td, th { padding: 6px 13px; border: 1px solid #ddd; text-align: left; }
    tr { background-color: #fff; border-top: 1px solid #ccc; }
    tr:nth-child(even) { background: #f8f8f8; }
</style>";

        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;
        private readonly DisassemblyDiagnoserConfig config;

        internal CombinedDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results, DisassemblyDiagnoserConfig config)
        {
            this.results = results;
            this.config = config;
        }

        protected override string FileExtension => "html";
        protected override string FileCaption => "disassembly-report";

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var benchmarksByTarget = summary.BenchmarksCases
                .Where(benchmark => results.ContainsKey(benchmark))
                .GroupBy(benchmark => benchmark.Descriptor.WorkloadMethod)
                .ToArray();

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
                        benchmark => summary[benchmark].GetRuntimeInfo());
                }
            }
            else // different methods, same JIT
            {
                PrintTable(
                    summary.BenchmarksCases.Where(benchmark => results.ContainsKey(benchmark)).ToArray(),
                    logger,
                    summary.Title,
                    benchmark => $"{benchmark.Descriptor.WorkloadMethod.Name} {summary[benchmark].GetRuntimeInfo()}");
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
                var disassemblyResult = results[benchmark];
                logger.WriteLine("<td style=\"vertical-align:top;\"><pre><code>");
                foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
                {
                    logger.WriteLine(method.Name);

                    var formatter = config.GetFormatterWithSymbolSolver(disassemblyResult.AddressToNameMapping);

                    foreach (var map in method.Maps)
                        foreach (var sourceCode in map.SourceCodes)
                            logger.WriteLine(CodeFormatter.Format(sourceCode, formatter, config.PrintInstructionAddresses, disassemblyResult.PointerSize, disassemblyResult.AddressToNameMapping));

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
    }
}