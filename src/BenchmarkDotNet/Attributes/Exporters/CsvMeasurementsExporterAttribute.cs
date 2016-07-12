using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class CsvMeasurementsExporterAttribute : ExporterConfigBaseAttribute
    {
        public CsvMeasurementsExporterAttribute() : base(DefaultExporters.CsvMeasurements)
        {
        }
    }
}