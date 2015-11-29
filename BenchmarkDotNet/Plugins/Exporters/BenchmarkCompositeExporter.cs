using System.Collections.Generic;
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

        public void ExportToFile(IList<BenchmarkReport> reports, string competitionName)
        {
            foreach (var exporter in exporters)
                exporter.ExportToFile(reports, competitionName);
        }
    }
}