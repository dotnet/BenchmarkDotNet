using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
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

        public void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
        {
            foreach (var exporter in exporters)
                exporter.Export(reports, logger, resultExtenders);
        }

        public void ExportToFile(IList<BenchmarkReport> reports, string competitionName, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
        {
            foreach (var exporter in exporters)
                exporter.ExportToFile(reports, competitionName, resultExtenders);
        }
    }
}