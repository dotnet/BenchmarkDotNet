using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;

using Perfolizer.Perfonar.Tables;

namespace BenchmarkDotNet.Exporters;

/// <summary>
/// IMPORTANT: Not fully implemented yet
/// </summary>
internal class PerfonarMdExporter : ExporterBase
{
    protected override string FileExtension => "perfonar.md";

    public override async ValueTask ExportAsync(Summary summary, CancelableStreamWriter writer, CancellationToken cancellationToken)
    {
        var table = new PerfonarTable(summary.ToPerfonar(), summary.GetDefaultTableConfig());
        string markdown = table.ToMarkdown(new PerfonarTableStyle());
        await writer.WriteLineAsync(markdown, cancellationToken).ConfigureAwait(false);
    }
}
