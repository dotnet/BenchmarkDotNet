using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.OpenMetrics;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class OpenMetricsExporterAttribute : ExporterConfigBaseAttribute
    {
        public OpenMetricsExporterAttribute() : base(OpenMetricsExporter.Default)
        {
        }
    }
}