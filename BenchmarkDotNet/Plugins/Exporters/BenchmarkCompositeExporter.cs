using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public class BenchmarkCompositeExporter : IBenchmarkExporter
    {
        public string Name => "composite";
        public string Description => "Composite exporter";

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

        public IEnumerable<string> ExportToFile(IList<BenchmarkReport> reports, string fileNamePrefix) => 
            exporters.SelectMany(exporter => exporter.ExportToFile(reports, fileNamePrefix));
    }
}