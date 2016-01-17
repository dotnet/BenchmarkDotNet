using BenchmarkDotNet.Plugins.Analyzers;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
using BenchmarkDotNet.Plugins.Toolchains;

namespace BenchmarkDotNet.Plugins
{
    public interface IBenchmarkPluginBuilder : IBenchmarkPlugins
    {
        IBenchmarkPluginBuilder AddLogger(IBenchmarkLogger logger);
        IBenchmarkPluginBuilder AddExporter(IBenchmarkExporter exporter);
        IBenchmarkPluginBuilder AddDiagnoser(IBenchmarkDiagnoser diagnoser);
        IBenchmarkPluginBuilder AddToolchain(IBenchmarkToolchainBuilder toolchainBuilder);
        IBenchmarkPluginBuilder AddAnalyser(IBenchmarkAnalyser analyser);
        IBenchmarkPluginBuilder AddResultExtender(IBenchmarkResultExtender extender);

        IBenchmarkPlugins Build();
    }
}