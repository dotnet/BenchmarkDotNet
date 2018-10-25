using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    public class PrettyGithubMarkdownDisassemblyExporter : PrettyGithubMarkdownDisassemblyExporterBase
    {
        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;

        public PrettyGithubMarkdownDisassemblyExporter(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results)
        {
            this.results = results;
        }

        protected override string FileExtension => "md";
        protected override string FileCaption => "asm.pretty";

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var benchmarksCases = summary.BenchmarksCases
                .Where(results.ContainsKey);

            foreach (var benchmarkCase in benchmarksCases)
            {
                logger.WriteLine($"## {GetImportantInfo(summary[benchmarkCase])}");
                Export(logger, results[benchmarkCase]);
            }
        }

        private static string GetImportantInfo(BenchmarkReport benchmarkReport) => benchmarkReport.GetRuntimeInfo();
    }
}