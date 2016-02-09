using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    public interface IBuilder
    {
        BuildResult Build(GenerateResult generateResult, ILogger logger);
    }
}