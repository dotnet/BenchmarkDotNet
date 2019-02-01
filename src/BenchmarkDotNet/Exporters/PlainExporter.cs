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
                var modeStages = runs.Select(it => (it.IterationMode, it.IterationStage)).Distinct();
                logger.WriteLineHeader($"*** {report.BenchmarkCase.DisplayInfo} ***");
                logger.WriteLineHeader("* Raw *");
                foreach (var run in runs)
                    logger.WriteLineResult(run.ToStr(report.BenchmarkCase.Config.Encoding));
                foreach (var (mode, stage) in modeStages)
                {
                    logger.WriteLine();
                    logger.WriteLineHeader($"* Statistics for {mode}{stage}");
                    logger.WriteLineStatistic(runs.Where(it => it.Is(mode, stage)).GetStatistics().ToTimeStr(report.BenchmarkCase.Config.Encoding, calcHistogram: true));
                }
            }
        }
    }
}