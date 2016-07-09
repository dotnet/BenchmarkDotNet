using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class BriefJsonExporterAttribute : ExporterConfigBaseAttribute
    {
        public BriefJsonExporterAttribute() : base(DefaultExporters.BriefJson)
        {
        }
    }
}