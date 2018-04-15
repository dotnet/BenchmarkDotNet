using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    public class InProcessBuilder : IBuilder
    {
        /// <summary>always returns success</summary>
        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
            => BuildResult.Success(generateResult);
    }
}