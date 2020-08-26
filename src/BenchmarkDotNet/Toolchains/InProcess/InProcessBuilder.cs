using System;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    [Obsolete("Please use BenchmarkDotNet.Toolchains.InProcess.NoEmit.* classes")]
    public class InProcessBuilder : IBuilder
    {
        /// <summary>always returns success</summary>
        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
            => BuildResult.Success(generateResult);
    }
}