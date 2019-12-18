using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class BenchmarkReportExporter: ExporterBase
    {
        public static readonly IExporter Default = new BenchmarkReportExporter();

        private BenchmarkReportExporter()
        {
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            foreach (var report in summary.Reports)
            {
                logger.WriteLineInfo(report.BenchmarkCase.DisplayInfo);
                logger.WriteLineStatistic($"Runtime = {report.GetRuntimeInfo()}; GC = {report.GetGcInfo()}");
                var resultRuns = report.GetResultRuns();
                if (resultRuns.IsEmpty())
                    logger.WriteLineError("There are not any results runs");
                else
                {
                    var statistics = resultRuns.GetStatistics();
                    var cultureInfo = summary.GetCultureInfo();
                    var formatter = statistics.CreateNanosecondFormatter(cultureInfo);
                    logger.WriteLineStatistic(statistics.ToString(cultureInfo, formatter, calcHistogram: true));
                }
                logger.WriteLine();
            }
        }
    }
}