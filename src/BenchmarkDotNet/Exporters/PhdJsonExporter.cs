using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Perfolizer.Json;

namespace BenchmarkDotNet.Exporters;

public class PhdJsonExporter(LightJsonSettings? jsonSettings = null) : ExporterBase
{
    protected override string FileExtension => "phd.json";

    public override void ExportToLog(Summary summary, ILogger logger)
    {
        logger.WriteLine(LightJsonSerializer.Serialize(summary.ToPhd(), jsonSettings));
    }
}