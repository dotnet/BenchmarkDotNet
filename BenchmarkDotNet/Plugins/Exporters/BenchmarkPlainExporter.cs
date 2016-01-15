using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public class BenchmarkPlainExporter : BenchmarkExporterBase
    {
        public override string Name => "txt";
        public override string Description => "Plain exporter";

        public static readonly IBenchmarkExporter Default = new BenchmarkPlainExporter();

        public override void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger)
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
    }
}