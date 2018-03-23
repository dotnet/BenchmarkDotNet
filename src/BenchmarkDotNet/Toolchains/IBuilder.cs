using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    public interface IBuilder
    {
        BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark, IResolver resolver);
    }
}