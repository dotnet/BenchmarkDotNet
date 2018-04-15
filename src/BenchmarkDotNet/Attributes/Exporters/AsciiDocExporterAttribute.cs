using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class AsciiDocExporterAttribute : ExporterConfigBaseAttribute
    {
        public AsciiDocExporterAttribute() : base(DefaultExporters.AsciiDoc)
        {
        }
    }
}