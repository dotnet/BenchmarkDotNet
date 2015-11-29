using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public class BenchmarkPlugins : IBenchmarkPlugins
    {
        private readonly List<IBenchmarkLogger> loggers = new List<IBenchmarkLogger>();
        private readonly List<IBenchmarkExporter> exporters = new List<IBenchmarkExporter>();
        private readonly List<IBenchmarkDiagnoser> diagnosers = new List<IBenchmarkDiagnoser>(); 

        public void AddLogger(IBenchmarkLogger logger) => loggers.Add(logger);
        public void AddExporter(IBenchmarkExporter exporter) => exporters.Add(exporter);
        public void AddDiagnoser(IBenchmarkDiagnoser diagnoser) => diagnosers.Add(diagnoser);

        public IBenchmarkLogger CompositeLogger => new BenchmarkCompositeLogger(loggers.ToArray());
        public IBenchmarkExporter CompositeExporter => new BenchmarkCompositeExporter(exporters.ToArray());
        public IBenchmarkDiagnoser CompositeDiagnoser => new BenchmarkCompositeDiagnoser(diagnosers.ToArray());
    }
}