using BenchmarkDotNet.Exporters.Csv;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class CsvExporterAttribute : ExporterConfigBaseAttribute
    {
        public CsvExporterAttribute(CsvSeparator separator = CsvSeparator.CurrentCulture) : base(new CsvExporter(separator))
        {
        }
    }
}