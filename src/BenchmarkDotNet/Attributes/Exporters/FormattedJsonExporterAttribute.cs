using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class FormattedJsonExporterAttribute : ExporterConfigBaseAttribute
    {
        public FormattedJsonExporterAttribute() : base(DefaultExporters.FormattedJson)
        {
        }
    }
}