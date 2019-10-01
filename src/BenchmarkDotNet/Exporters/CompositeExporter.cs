using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class CompositeExporter : IExporter
    {
        private readonly ImmutableArray<IExporter> exporters;

        public CompositeExporter(ImmutableArray<IExporter> exporters) => this.exporters = exporters;

        public string Name => nameof(CompositeExporter);

        public void ExportToLog(Summary summary, ILogger logger)
        {
            if(summary.GetColumns().IsNullOrEmpty())
                logger.WriteLineHint("You haven't configured any columns, your results will be empty");

            foreach (var exporter in exporters)
                exporter.ExportToLog(summary, logger);
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
            => exporters.SelectMany(exporter => exporter.ExportToFiles(summary, consoleLogger));
    }
}