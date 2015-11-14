using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Toolchain.Results;

namespace BenchmarkDotNet.Toolchain
{
    internal interface IBenchmarkExecutor
    {
        BenchmarkExecResult Exec(BenchmarkBuildResult buildResult, BenchmarkParameters parameters);
    }
}