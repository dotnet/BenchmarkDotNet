using BenchmarkDotNet.Helpers;
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

        public override async ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken)
        {
            await writer.WriteLineAsync("....", cancellationToken).ConfigureAwait(false);
            foreach (string infoLine in summary.HostEnvironmentInfo.ToFormattedString())
            {
                await writer.WriteLineAsync(infoLine, cancellationToken).ConfigureAwait(false);
            }
            await writer.WriteLineAsync(summary.AllRuntimes, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);

            var table = summary.Table;
            if (table.FullContent.Length == 0)
            {
                await writer.WriteLineAsync("[WARNING]", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("====", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("There are no benchmarks found ", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("====", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var wrappedWriter = new StreamWriterWrapper(writer);
            await table.PrintCommonColumnsAsync(wrappedWriter, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("....", cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("[options=\"header\"]", cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("|===", cancellationToken).ConfigureAwait(false);
            await table.PrintLineAsync(table.FullHeader, wrappedWriter, "|", string.Empty, cancellationToken).ConfigureAwait(false);
            foreach (var line in table.FullContent)
                await table.PrintLineAsync(line, wrappedWriter, "|", string.Empty, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync("|===", cancellationToken).ConfigureAwait(false);

            var benchmarksWithTroubles = summary.Reports
                .Where(r => !r.GetResultRuns().Any())
                .Select(r => r.BenchmarkCase)
                .ToList();

            if (benchmarksWithTroubles.Count > 0)
            {
                await writer.WriteLineAsync(cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("[WARNING]", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync(".Benchmarks with issues", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("====", cancellationToken).ConfigureAwait(false);
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                    await writer.WriteLineAsync($"* {benchmarkWithTroubles.DisplayInfo}", cancellationToken).ConfigureAwait(false);
                await writer.WriteLineAsync("====", cancellationToken).ConfigureAwait(false);
            }
        }
    }
}