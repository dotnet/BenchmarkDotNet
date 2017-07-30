using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class DisassemblyExporter : ExporterBase
    {
        private readonly Dictionary<Benchmark, string> results;

        public DisassemblyExporter(Dictionary<Benchmark, string> results)
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
            logger.WriteLine("<title> DisassemblyDiagnoser Output " + summary.Title + "</title>");
            logger.WriteLine(HtmlExporter.CssDefinition);
            logger.WriteLine("</head>");

            logger.WriteLine("<body>");

            foreach (var targetingSameMethod in benchmarksByTarget)
            {
                PrintTable(targetingSameMethod.ToArray(), logger);
            }

            logger.WriteLine("</body>");
            logger.WriteLine("</html>");
        }

        private void PrintTable(Benchmark[] benchmarks, ILogger logger)
        {
            logger.WriteLine("<table>");
            logger.WriteLine("<thead>");
            logger.WriteLine($"<tr><th colspan=\"{benchmarks.Length}\">{benchmarks[0].Target.DisplayInfo}</th></tr>");
            logger.WriteLine("<tr>");
            foreach (var benchmark in benchmarks)
            {
                logger.WriteLine($"<th>{GetImportantInfo(benchmark.Job)}</th>");
            }
            logger.WriteLine("</tr>");
            logger.WriteLine("</thead>");
            
            logger.WriteLine("<tbody>");
            logger.WriteLine("<tr>");
            foreach (var benchmark in benchmarks)
            {
                logger.WriteLine($"<td style=\"vertical-align:top;\"><pre><code>{results[benchmark]}</code></pre></td>");
            }
            logger.WriteLine("</tr>");
            logger.WriteLine("</tbody>");
            logger.WriteLine("</table>");
        }

        private string GetImportantInfo(Job job) => $"{job.Env.Jit} {job.Env.Platform} {job.Env.Runtime?.Name} {job.Infrastructure.Toolchain?.Name}";
    }
}