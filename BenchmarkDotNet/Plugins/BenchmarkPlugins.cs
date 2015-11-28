using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Diagnosters;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public class BenchmarkPlugins : IBenchmarkPlugins
    {
        private readonly List<IBenchmarkLogger> loggers = new List<IBenchmarkLogger>();
        private readonly List<IBenchmarkExporter> exporters = new List<IBenchmarkExporter>();
        private readonly List<IBenchmarkDiagnoster> diagnosters = new List<IBenchmarkDiagnoster>(); 

        public void AddLogger(IBenchmarkLogger logger) => loggers.Add(logger);
        public void AddExporter(IBenchmarkExporter exporter) => exporters.Add(exporter);
        public void AddDiagnoster(IBenchmarkDiagnoster diagnoster) => diagnosters.Add(diagnoster);

        public IBenchmarkLogger CompositeLogger => new BenchmarkCompositeLogger(loggers.ToArray());
        public IBenchmarkExporter CompositeExporter => new BenchmarkCompositeExporter(exporters.ToArray());
        public IBenchmarkDiagnoster CompositeDiagnoster => new BenchmarkCompositeDiagnoster(diagnosters.ToArray());
    }
}