using BenchmarkDotNet.Plugins.Toolchains.Results;

namespace BenchmarkDotNet.Plugins.Toolchains
{
    public interface IBenchmarkBuilder
    {
        BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult, Benchmark benchmark);
    }
}