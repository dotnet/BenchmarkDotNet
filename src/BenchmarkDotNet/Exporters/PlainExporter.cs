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
            var cultureInfo = summary.GetCultureInfo();
            foreach (var report in summary.Reports)
            {
                var measurements = report.AllMeasurements;
                var modeStages = measurements.Select(it => (it.IterationMode, it.IterationStage)).Distinct();
                logger.WriteLineHeader($"*** {report.BenchmarkCase.DisplayInfo} ***");
                logger.WriteLineHeader("* Raw *");
                foreach (var measurement in measurements)
                    logger.WriteLineResult(measurement.ToString());
                foreach (var (mode, stage) in modeStages)
                {
                    logger.WriteLine();
                    logger.WriteLineHeader($"* Statistics for {mode}{stage}");
                    var statistics = measurements.Where(it => it.Is(mode, stage)).GetStatistics();
                    var formatter = statistics.CreateNanosecondFormatter(cultureInfo);
                    logger.WriteLineStatistic(statistics.ToString(cultureInfo, formatter, calcHistogram: true));
                }
            }
        }
    }
}