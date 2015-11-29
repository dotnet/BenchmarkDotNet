using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public interface IBenchmarkPluginBuilder : IBenchmarkPlugins
    {
        IBenchmarkPluginBuilder AddLogger(IBenchmarkLogger logger);
        IBenchmarkPluginBuilder AddExporter(IBenchmarkExporter exporter);
        IBenchmarkPluginBuilder AddDiagnoser(IBenchmarkDiagnoser diagnoser);

        IBenchmarkPlugins Build();
    }
}