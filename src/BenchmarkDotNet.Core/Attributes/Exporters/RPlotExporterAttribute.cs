using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
#if !UAP
    public class RPlotExporterAttribute : ExporterConfigBaseAttribute
    {
        public RPlotExporterAttribute() : base(DefaultExporters.RPlot)
        {
        }
    }
#endif
}