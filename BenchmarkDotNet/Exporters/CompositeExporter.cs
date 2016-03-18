using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class CompositeExporter : IExporter
    {
        private readonly IExporter[] exporters;

        public CompositeExporter(params IExporter[] exporters)
        {
            this.exporters = exporters;
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            foreach (var exporter in exporters)
                exporter.ExportToLog(summary, logger);
        }

        public IEnumerable<string> ExportToFiles(Summary summary)
        {
            return exporters.SelectMany(exporter => exporter.ExportToFiles(summary));
        }
    }
}