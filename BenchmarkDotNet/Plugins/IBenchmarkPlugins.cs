using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public interface IBenchmarkPlugins
    {
        void AddLogger(IBenchmarkLogger logger);
        void AddExporter(IBenchmarkExporter exporter);
        void AddDiagnoser(IBenchmarkDiagnoser diagnoser);

        IBenchmarkLogger CompositeLogger { get; }
        IBenchmarkExporter CompositeExporter { get; }
        IBenchmarkDiagnoser CompositeDiagnoser { get; }
    }
}