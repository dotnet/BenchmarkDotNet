using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.Snapshot;
using System.Collections.Generic;

namespace BenchmarkDotNet.Exporters
{
    public class SnapshotExporter : IExporter
    {
        private readonly ISnapshotStore _store;

        public static IExporter From(ISnapshotStore store)
        {
            return new SnapshotExporter(store);
        }

        private SnapshotExporter(ISnapshotStore store)
        {
            this._store = store;
            Name = store.Name + "Exporter";
        }

        public string Name { get; }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger logger)
        {
            try
            {
                //System.Diagnostics.Debugger.Break();
                _store.ExportBegin(logger);
                ExportToLog(summary, logger);
            }
            finally
            {
                _store.ExportEnd(logger);
            }
            return new[] { _store.Filename };
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
            _store.Export(summary, logger);
        }
    }
}
