using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class MarkdownExporterAttribute : ExporterConfigBaseAttribute
    {
        public MarkdownExporterAttribute() : base(DefaultExporters.Markdown)
        {
        }
    }
}