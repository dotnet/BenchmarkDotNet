using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Perfolizer.Json;
using Perfolizer.Phd.Presenting;
using Perfolizer.Phd.Tables;
using Perfolizer.Presenting;

namespace BenchmarkDotNet.Exporters;

/// <summary>
/// IMPORTANT: Not fully implemented yet
/// </summary>
public class PhdMdExporter : ExporterBase
{
    protected override string FileExtension => "phd.md";

    public override void ExportToLog(Summary summary, ILogger logger)
    {
        var table = new PhdTable(summary.ToPhd());
        var presenter = new StringPresenter();
        new PhdMarkdownTablePresenter(presenter).Present(table, new PhdTableStyle());
        logger.WriteLine(presenter.Dump());
    }
}