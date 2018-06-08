using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes
{
    public class PlainExporterAttribute : ExporterConfigBaseAttribute
    {
        public PlainExporterAttribute() : base(DefaultExporters.Plain)
        {
        }
    }
}