using BenchmarkDotNet.Plugins.Analyzers;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.Toolchains;

namespace BenchmarkDotNet.Plugins
{
    public interface IBenchmarkPlugins
    {
        IBenchmarkLogger CompositeLogger { get; }
        IBenchmarkExporter CompositeExporter { get; }
        IBenchmarkDiagnoser CompositeDiagnoser { get; }
        IBenchmarkAnalyser CompositeAnalyser { get; }
        IBenchmarkToolchainFacade CreateToolchain(Benchmark benchmark, IBenchmarkLogger logger);
    }
}