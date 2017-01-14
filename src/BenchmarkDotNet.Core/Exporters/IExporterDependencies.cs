using System.Collections.Generic;

namespace BenchmarkDotNet.Exporters
{
    /// <summary>
    /// This is an internal interface, it allows Exporters to specify that
    /// they depends on another Exporter (see RPlotExporter for example)
    /// </summary>
    internal interface IExporterDependencies
    {
        IEnumerable<IExporter> Dependencies { get; }
    }
}
