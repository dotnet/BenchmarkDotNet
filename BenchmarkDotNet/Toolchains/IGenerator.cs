using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    public interface IGenerator
    {
        GenerateResult GenerateProject(Benchmark benchmark, ILogger logger);
    }
}