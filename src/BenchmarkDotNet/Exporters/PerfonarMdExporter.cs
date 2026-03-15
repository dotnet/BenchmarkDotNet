using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

using Perfolizer.Perfonar.Tables;

namespace BenchmarkDotNet.Exporters;

/// <summary>
/// IMPORTANT: Not fully implemented yet
/// </summary>
internal class PerfonarMdExporter : ExporterBase
{
    protected override string FileExtension => "perfonar.md";

    protected override async ValueTask ExportAsync(Summary summary, StreamOrLoggerWriter writer, CancellationToken cancellationToken)
    {
        var table = new PerfonarTable(summary.ToPerfonar(), summary.GetDefaultTableConfig());
        string markdown = table.ToMarkdown(new PerfonarTableStyle());
        await writer.WriteLineAsync(markdown, cancellationToken).ConfigureAwait(false);
    }
}
