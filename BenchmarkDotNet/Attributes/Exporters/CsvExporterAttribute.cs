using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class CsvExporterAttribute : ExporterConfigBaseAttribute
    {
        public CsvExporterAttribute() : base(DefaultExporters.Csv)
        {
        }
    }
}