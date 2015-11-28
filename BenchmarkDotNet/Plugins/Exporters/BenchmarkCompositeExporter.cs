using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public class BenchmarkCompositeExporter : IBenchmarkExporter
    {
        private readonly IBenchmarkExporter[] exporters;

        public BenchmarkCompositeExporter(params IBenchmarkExporter[] exporters)
        {
            this.exporters = exporters;
        }

        public void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger)
        {
            foreach (var exporter in exporters)
                exporter.Export(reports, logger);
        }
    }
}