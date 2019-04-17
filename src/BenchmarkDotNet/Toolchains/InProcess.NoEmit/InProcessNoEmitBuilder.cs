using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /// <summary>
    /// In process (no emit) toolchain builder
    /// </summary>
    /// <seealso cref="BenchmarkDotNet.Toolchains.IBuilder" />
    public class InProcessNoEmitBuilder : IBuilder
    {
        /// <summary>always returns success</summary>
        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
            => BuildResult.Success(generateResult);
    }
}