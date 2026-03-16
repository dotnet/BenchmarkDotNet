using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class AsciiDocExporter : ExporterBase
    {
        protected override string FileExtension => "asciidoc";

        public static readonly IExporter Default = new AsciiDocExporter();

        private AsciiDocExporter()
        {
        }

        protected override async ValueTask ExportAsync(Summary summary, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
        {
            await writer.WriteLineAsync("....", cancellationToken).ConfigureAwait(false);
            foreach (string infoLine in summary.HostEnvironmentInfo.ToFormattedString())
            {
                await writer.WriteLineAsync(infoLine, LogKind.Info, cancellationToken).ConfigureAwait(false);
            }
            await writer.WriteLineAsync(summary.AllRuntimes, LogKind.Info, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);

            var table = summary.Table;
            if (table.FullContent.Length == 0)
            {
                await writer.WriteLineAsync("[WARNING]", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("====", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("There are no benchmarks found ", LogKind.Error, cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("====", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await table.PrintCommonColumnsAsync(writer, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("....", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("[options=\"header\"]", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("|===", cancellationToken).ConfigureAwait(false);
            await table.PrintLineAsync(table.FullHeader, writer, "|", string.Empty, cancellationToken).ConfigureAwait(false);
            foreach (var line in table.FullContent)
                await table.PrintLineAsync(line, writer, "|", string.Empty, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("|===", cancellationToken).ConfigureAwait(false);

            var benchmarksWithTroubles = summary.Reports
                .Where(r => !r.GetResultRuns().Any())
                .Select(r => r.BenchmarkCase)
                .ToList();

            if (benchmarksWithTroubles.Count > 0)
            {
                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("[WARNING]", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync(".Benchmarks with issues", LogKind.Error, cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("====", cancellationToken).ConfigureAwait(false);
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                    await writer.WriteLineAsync($"* {benchmarkWithTroubles.DisplayInfo}", LogKind.Error, cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("====", cancellationToken).ConfigureAwait(false);
            }
        }
    }
}