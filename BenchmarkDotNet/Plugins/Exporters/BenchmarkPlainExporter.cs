using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public class BenchmarkPlainExporter : IBenchmarkExporter
    {
        public string Name => "txt";
        public string Description => "Plain exporter";

        public static readonly IBenchmarkExporter Default = new BenchmarkPlainExporter();

        public void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger)
        {
            foreach (var report in reports)
            {
                var runs = report.Runs;
                var modes = runs.Select(it => it.IterationMode).Distinct();
                logger.WriteLineHeader($"*** {report.Benchmark.Description} ***");
                logger.WriteLineHeader("* Raw *");
                foreach (var run in runs)
                    logger.WriteLineResult(run.ToStr());
                foreach (var mode in modes)
                {
                    logger.NewLine();
                    logger.WriteLineHeader($"* Statistics for {mode}");
                    logger.WriteLineStatistic(runs.Where(it => it.IterationMode == mode).GetStats().ToTimeStr());
                }
            }
        }

        public IEnumerable<string> ExportToFile(IList<BenchmarkReport> reports, string fileNamePrefix)
        {
            yield return BenchmarkExporterHelper.ExportToFile(this, reports, fileNamePrefix);
        }
    }
}