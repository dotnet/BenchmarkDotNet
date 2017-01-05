using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class CompositeExporter : IExporter
    {
        internal readonly IEnumerable<IExporter> exporters;

        public CompositeExporter(params IExporter[] exporters)
        {
            // Start with all the Exporters we were given
            var tempList = new List<IExporter>(exporters);

            // Now fetch their dependencies (if any) and add them if they AREN'T already present
            foreach (var exporter in exporters.OfType<IExporterDependencies>())
            {
                foreach (var dependency in exporter.Dependencies)
                {
                    if (exporters.Contains(dependency) == false)
                        tempList.Add(dependency);
                }
            }

            this.exporters = tempList;
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            foreach (var exporter in exporters)
                exporter.ExportToLog(summary, logger);
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            return exporters.SelectMany(exporter => exporter.ExportToFiles(summary, consoleLogger));
        }
    }
}