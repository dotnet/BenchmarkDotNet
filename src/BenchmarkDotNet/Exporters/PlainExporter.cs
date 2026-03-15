using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        protected override async ValueTask ExportAsync(Summary summary, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
        {
            var cultureInfo = summary.GetCultureInfo();
            foreach (var report in summary.Reports)
            {
                var measurements = report.AllMeasurements;
                var modeStages = measurements.Select(it => (it.IterationMode, it.IterationStage)).Distinct();
                await writer.WriteLineAsync($"*** {report.BenchmarkCase.DisplayInfo} ***", LogKind.Header, cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("* Raw *", LogKind.Header, cancellationToken).ConfigureAwait(false);
                foreach (var measurement in measurements)
                    await writer.WriteLineAsync(measurement.ToString(), LogKind.Result, cancellationToken).ConfigureAwait(false);
                foreach (var (mode, stage) in modeStages)
                {
                    await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                    await writer.WriteLineAsync($"* Statistics for {mode}{stage}", LogKind.Header, cancellationToken).ConfigureAwait(false);
                    var statistics = measurements.Where(it => it.Is(mode, stage)).GetStatistics();
                    var formatter = statistics.CreateNanosecondFormatter(cultureInfo);
                    await writer.WriteLineAsync(statistics.ToString(cultureInfo, formatter, calcHistogram: true), LogKind.Statistic, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
