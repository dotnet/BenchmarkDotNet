using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Perfolizer.Json;

namespace BenchmarkDotNet.Exporters;

/// <summary>
/// IMPORTANT: Not fully implemented yet
/// </summary>
internal class PerfonarJsonExporter(LightJsonSettings? jsonSettings = null) : ExporterBase
{
    protected override string FileExtension => "perfonar.json";

    protected override async ValueTask ExportAsync(Summary summary, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteLineAsync(LightJsonSerializer.Serialize(summary.ToPerfonar(), jsonSettings), cancellationToken).ConfigureAwait(false);
    }
}
