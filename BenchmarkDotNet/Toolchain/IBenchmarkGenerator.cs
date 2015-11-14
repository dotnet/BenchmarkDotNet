using BenchmarkDotNet.Toolchain.Results;

namespace BenchmarkDotNet.Toolchain
{
    internal interface IBenchmarkGenerator
    {
        BenchmarkGenerateResult GenerateProject(Benchmark benchmark);
    }
}