using BenchmarkDotNet.Flow.Results;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Flow
{
    internal interface IBenchmarkFlow
    {
        BenchmarkGenerateResult Generate();
        BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult);
        BenchmarkExecResult Exec(BenchmarkBuildResult buildResult, BenchmarkParameters parameters);
    }
}