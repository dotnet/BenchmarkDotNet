using BenchmarkDotNet.Plugins.Toolchains.Results;

namespace BenchmarkDotNet.Plugins.Toolchains
{
    public interface IBenchmarkGenerator
    {
        BenchmarkGenerateResult GenerateProject(Benchmark benchmark);
    }
}