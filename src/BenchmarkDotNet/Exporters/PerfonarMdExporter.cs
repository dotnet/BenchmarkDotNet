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

    public override void ExportToLog(Summary summary, ILogger logger)
    {
        var table = new PerfonarTable(summary.ToPerfonar(), summary.GetDefaultTableConfig());
        string markdown = table.ToMarkdown(new PerfonarTableStyle());
        logger.WriteLine(markdown);
    }
}