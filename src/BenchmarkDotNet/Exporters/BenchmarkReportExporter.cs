using System.Threading;
using System.Threading.Tasks;
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

        protected override async ValueTask ExportAsync(Summary summary, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
        {
            foreach (var report in summary.Reports)
            {
                await writer.WriteLineAsync(report.BenchmarkCase.DisplayInfo, LogKind.Info, cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync($"Runtime = {report.GetRuntimeInfo()}; GC = {report.GetGcInfo()}", LogKind.Statistic, cancellationToken).ConfigureAwait(false);
                var resultRuns = report.GetResultRuns();
                if (resultRuns.IsEmpty())
                    await writer.WriteLineAsync("There are not any results runs", LogKind.Error, cancellationToken).ConfigureAwait(false);
                else
                {
                    var statistics = resultRuns.GetStatistics();
                    var cultureInfo = summary.GetCultureInfo();
                    var formatter = statistics.CreateNanosecondFormatter(cultureInfo);
                    await writer.WriteLineAsync(statistics.ToString(cultureInfo, formatter, calcHistogram: true), LogKind.Statistic, cancellationToken).ConfigureAwait(false);
                }
                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
