using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Toolchain.Results;

namespace BenchmarkDotNet.Toolchain
{
    internal interface IBenchmarkToolchainFacade
    {
        BenchmarkGenerateResult Generate();
        BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult);
        BenchmarkExecResult Exec(BenchmarkBuildResult buildResult, BenchmarkParameters parameters, IBenchmarkDiagnoser diagnoser);
    }
}