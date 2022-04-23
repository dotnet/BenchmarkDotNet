using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using System.Diagnostics;

namespace BenchmarkDotNet.Toolchains
{
    public interface IExecutor
    {
        ExecuteResult Execute(ExecuteParameters executeParameters);

        ProcessStartInfo GetProcessStartInfo(ExecuteParameters executeParameters);
    }
}