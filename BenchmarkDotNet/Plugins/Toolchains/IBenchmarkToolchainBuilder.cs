using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Plugins.Toolchains;

namespace BenchmarkDotNet.Plugins.Toolchains
{
    public interface IBenchmarkToolchainBuilder
    {
        IBenchmarkToolchainFacade Build(Benchmark benchmark, IBenchmarkLogger logger);
        BenchmarkToolchain TargetToolchain { get; }
    }
}