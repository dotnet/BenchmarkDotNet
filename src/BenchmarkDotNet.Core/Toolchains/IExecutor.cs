using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    public interface IExecutor
    {
        ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IDiagnoser diagnoser = null);
    }
}