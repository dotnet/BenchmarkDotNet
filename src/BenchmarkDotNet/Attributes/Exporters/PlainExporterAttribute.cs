using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class PlainExporterAttribute : ExporterConfigBaseAttribute
    {
        public PlainExporterAttribute() : base(DefaultExporters.Plain)
        {
        }
    }
}