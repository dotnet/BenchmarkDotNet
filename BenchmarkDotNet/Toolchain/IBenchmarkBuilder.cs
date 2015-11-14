using BenchmarkDotNet.Toolchain.Results;

namespace BenchmarkDotNet.Toolchain
{
    internal interface IBenchmarkBuilder
    {
        BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult);
    }
}