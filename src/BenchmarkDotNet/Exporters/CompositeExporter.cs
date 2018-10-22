using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class CompositeExporter : IExporter
    {
        internal readonly IEnumerable<IExporter> Exporters;
        public string Name => nameof(CompositeExporter);

        public CompositeExporter(params IExporter[] exporters)
        {
            var allExporters = new List<IExporter>(exporters.Length * 2);

            void AddExporter(IExporter newExporter)
            {
                // All the exporter dependencies should be added before the exporter
                var dependencies = (newExporter as IExporterDependencies)?.Dependencies;
                if (dependencies != null)
                    foreach (var dependency in dependencies)
                        AddExporter(dependency);

                if (!allExporters.Contains(newExporter)) // TODO: Exporters should be matched by Id
                    allExporters.Add(newExporter);
            }

            foreach (var exporter in exporters)
                AddExporter(exporter);

            Exporters = allExporters;
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            if(summary.GetColumns().IsNullOrEmpty())
                logger.WriteLineHint("You haven't configured any columns, your results will be empty");

            foreach (var exporter in Exporters)
                exporter.ExportToLog(summary, logger);
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            return Exporters.SelectMany(exporter => exporter.ExportToFiles(summary, consoleLogger));
        }
    }
}