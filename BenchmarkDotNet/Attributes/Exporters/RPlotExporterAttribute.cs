using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class RPlotExporterAttribute : ExporterConfigBaseAttribute
    {
        public RPlotExporterAttribute() : base(DefaultExporters.RPlot)
        {
        }
    }
}