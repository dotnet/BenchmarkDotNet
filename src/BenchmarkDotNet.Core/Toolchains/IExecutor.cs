using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    public interface IExecutor
    {
        ExecuteResult Execute(ExecuteParameters executeParameters);
    }
}