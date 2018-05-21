using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class PlainExporter : ExporterBase
    {
        public static readonly IExporter Default = new PlainExporter();

        private PlainExporter()
        {
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            foreach (var report in summary.Reports)
            {
                var runs = report.AllMeasurements;
                var modes = runs.Select(it => it.IterationMode).Distinct();
                logger.WriteLineHeader($"*** {report.Benchmark.DisplayInfo} ***");
                logger.WriteLineHeader("* Raw *");
                foreach (var run in runs)
                    logger.WriteLineResult(run.ToStr());
                foreach (var mode in modes)
                {
                    logger.WriteLine();
                    logger.WriteLineHeader($"* Statistics for {mode}");
                    logger.WriteLineStatistic(runs.Where(it => it.IterationMode == mode).GetStatistics().ToTimeStr(calcHistogram: true));
                }
            }
        }
    }
}