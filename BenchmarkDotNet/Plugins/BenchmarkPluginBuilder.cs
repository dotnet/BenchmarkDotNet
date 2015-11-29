using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public class BenchmarkPluginBuilder : IBenchmarkPluginBuilder
    {
        private readonly List<IBenchmarkLogger> loggers = new List<IBenchmarkLogger>();
        private readonly List<IBenchmarkExporter> exporters = new List<IBenchmarkExporter>();
        private readonly List<IBenchmarkDiagnoser> diagnosers = new List<IBenchmarkDiagnoser>();

        public IBenchmarkPluginBuilder AddLogger(IBenchmarkLogger logger)
        {
            loggers.Add(logger);
            return this;
        }

        public IBenchmarkPluginBuilder AddExporter(IBenchmarkExporter exporter)
        {
            exporters.Add(exporter);
            return this;
        }

        public IBenchmarkPluginBuilder AddDiagnoser(IBenchmarkDiagnoser diagnoser)
        {
            diagnosers.Add(diagnoser);
            return this;
        }

        public IBenchmarkLogger CompositeLogger => new BenchmarkCompositeLogger(loggers.ToArray());
        public IBenchmarkExporter CompositeExporter => new BenchmarkCompositeExporter(exporters.ToArray());
        public IBenchmarkDiagnoser CompositeDiagnoser => new BenchmarkCompositeDiagnoser(diagnosers.ToArray());

        public IBenchmarkPlugins Build() => this;
    }
}