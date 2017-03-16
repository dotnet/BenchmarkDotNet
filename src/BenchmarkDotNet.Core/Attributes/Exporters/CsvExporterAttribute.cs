using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class CsvExporterAttribute : ExporterConfigBaseAttribute
    {
        public CsvExporterAttribute(CsvSeparator separator = CsvSeparator.CurrentCulture) : base(new CsvExporter(separator, SummaryStyle.Default))
        {
        }
    }
}