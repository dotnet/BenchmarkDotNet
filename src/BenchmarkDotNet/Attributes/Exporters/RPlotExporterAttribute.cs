using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes
{
    public class RPlotExporterAttribute : ExporterConfigBaseAttribute
    {
        public RPlotExporterAttribute() : base(DefaultExporters.RPlot)
        {
        }
    }
}