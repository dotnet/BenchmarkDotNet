using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    public class CombinedDisassemblyExporter : ExporterBase
    {
        private readonly IReadOnlyDictionary<Benchmark, DisassemblyResult> results;

        public CombinedDisassemblyExporter(IReadOnlyDictionary<Benchmark, DisassemblyResult> results)
        {
            this.results = results;
        }

        protected override string FileExtension => "html";
        protected override string FileCaption => "disassembly-report";

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var benchmarksByTarget = summary.Benchmarks
                .Where(benchmark => results.ContainsKey(benchmark))
                .GroupBy(benchmark => benchmark.Target.Method);

            logger.WriteLine("<!DOCTYPE html>");
            logger.WriteLine("<html lang='en'>");
            logger.WriteLine("<head>");
            logger.WriteLine("<meta charset='utf-8' />");
            logger.WriteLine($"<title>DisassemblyDiagnoser Output {summary.Title}</title>");
            logger.WriteLine(HtmlExporter.CssDefinition);
            logger.WriteLine("</head>");

            logger.WriteLine("<body>");

            if (benchmarksByTarget.Any(group => group.Count() > 1)) // the user is comparing same method for different JITs
            {
                foreach (var targetingSameMethod in benchmarksByTarget)
                {
                    PrintTable(
                        targetingSameMethod.ToArray(), 
                        logger, 
                        targetingSameMethod.First().Target.DisplayInfo, 
                        benchmark => GetImportantInfo(benchmark.Job));
                }
            }
            else // different methods, same JIT
            {
                PrintTable(
                    summary.Benchmarks.Where(benchmark => results.ContainsKey(benchmark)).ToArray(), 
                    logger, 
                    summary.Title, 
                    benchmark => $"{benchmark.Target.Method.Name} {GetImportantInfo(benchmark.Job)}");
            }

            logger.WriteLine("</body>");
            logger.WriteLine("</html>");
        }

        private void PrintTable(Benchmark[] benchmarks, ILogger logger, string title, Func<Benchmark, string> headerTitleProvider)
        {
            logger.WriteLine("<table>");
            logger.WriteLine("<thead>");
            logger.WriteLine($"<tr><th colspan=\"{benchmarks.Length}\">{title}</th></tr>");
            logger.WriteLine("<tr>");
            foreach (var benchmark in benchmarks)
            {
                logger.WriteLine($"<th>{headerTitleProvider(benchmark)}</th>");
            }
            logger.WriteLine("</tr>");
            logger.WriteLine("</thead>");
            
            logger.WriteLine("<tbody>");
            logger.WriteLine("<tr>");
            foreach (var benchmark in benchmarks)
            {
                logger.WriteLine("<td style=\"vertical-align:top;\"><pre><code>");
                foreach (var method in results[benchmark].Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
                {
                    logger.WriteLine($"{SingleDisassemblyExporter.FormatMethodAddress(method.NativeCode)} {method.Name}");

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

        private string GetImportantInfo(Job job) => $"{job.Env.Jit} {job.Env.Platform} {job.Env.Runtime?.Name} {job.Infrastructure.Toolchain?.Name}";
    }
}