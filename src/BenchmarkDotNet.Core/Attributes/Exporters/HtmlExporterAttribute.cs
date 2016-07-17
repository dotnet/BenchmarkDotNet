using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class HtmlExporterAttribute : ExporterConfigBaseAttribute
    {
        public HtmlExporterAttribute() : base(DefaultExporters.Html)
        {
        }
    }
}