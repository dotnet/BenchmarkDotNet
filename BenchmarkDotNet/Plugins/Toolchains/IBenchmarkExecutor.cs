using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Toolchains.Results;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Plugins.Toolchains
{
    public interface IBenchmarkExecutor
    {
        BenchmarkExecResult Exec(BenchmarkBuildResult buildResult, BenchmarkParameters parameters, IBenchmarkDiagnoser diagnoser);
    }
}