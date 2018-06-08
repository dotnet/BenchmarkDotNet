using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes
{
    public class HtmlExporterAttribute : ExporterConfigBaseAttribute
    {
        public HtmlExporterAttribute() : base(DefaultExporters.Html)
        {
        }
    }
}