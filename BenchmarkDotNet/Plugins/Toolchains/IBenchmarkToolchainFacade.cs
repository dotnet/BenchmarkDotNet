using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Toolchains.Results;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Plugins.Toolchains
{
    public interface IBenchmarkToolchainFacade
    {
        BenchmarkGenerateResult Generate();
        BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult);
        BenchmarkExecResult Execute(BenchmarkBuildResult buildResult, BenchmarkParameters parameters, IBenchmarkDiagnoser diagnoser);
    }
}