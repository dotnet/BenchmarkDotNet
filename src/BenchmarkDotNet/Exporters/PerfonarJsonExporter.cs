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

    public override void ExportToLog(Summary summary, ILogger logger)
    {
        logger.WriteLine(LightJsonSerializer.Serialize(summary.ToPerfonar(), jsonSettings));
    }
}