using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes
{
    public class AsciiDocExporterAttribute : ExporterConfigBaseAttribute
    {
        public AsciiDocExporterAttribute() : base(DefaultExporters.AsciiDoc)
        {
        }
    }
}