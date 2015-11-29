using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Plugins.Toolchains;

namespace BenchmarkDotNet.Plugins
{
    public interface IBenchmarkPlugins
    {
        IBenchmarkLogger CompositeLogger { get; }
        IBenchmarkExporter CompositeExporter { get; }
        IBenchmarkDiagnoser CompositeDiagnoser { get; }
        IBenchmarkToolchainFacade CreateToolchain(Benchmark benchmark, IBenchmarkLogger logger);
    }
}