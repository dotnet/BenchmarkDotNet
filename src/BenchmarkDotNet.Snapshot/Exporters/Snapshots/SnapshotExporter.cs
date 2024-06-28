using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.Snapshot;
using System.Collections.Generic;

namespace BenchmarkDotNet.Exporters
{
    /// <summary>
    ///
    /// </summary>
    public class SnapshotExporter : IExporter
    {
        private readonly ISnapshotStore _store;

        /// <summary>
        ///
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        public static IExporter From(ISnapshotStore store)
        {
            return new SnapshotExporter(store);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="store"></param>
        private SnapshotExporter(ISnapshotStore store)
        {
            this._store = store;
            Name = store.Name + "Exporter";
        }

        /// <summary>
        ///
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="logger"></param>
        public void ExportToLog(Summary summary, ILogger logger)
        {
            _store.Export(summary, logger);
        }
    }
}
