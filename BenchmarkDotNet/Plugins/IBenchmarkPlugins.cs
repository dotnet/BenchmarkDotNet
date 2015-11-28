using BenchmarkDotNet.Plugins.Diagnosters;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public interface IBenchmarkPlugins
    {
        void AddLogger(IBenchmarkLogger logger);
        void AddExporter(IBenchmarkExporter exporter);
        void AddDiagnoster(IBenchmarkDiagnoster diagnoster);

        IBenchmarkLogger CompositeLogger { get; }
        IBenchmarkExporter CompositeExporter { get; }
        IBenchmarkDiagnoster CompositeDiagnoster { get; }
    }
}