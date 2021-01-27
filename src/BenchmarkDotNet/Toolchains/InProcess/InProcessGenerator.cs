using System;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>
    /// Implementation of <see cref="IGenerator"/> for in-process benchmarks.
    /// </summary>
    [Obsolete("Please use BenchmarkDotNet.Toolchains.InProcess.NoEmit.* classes")]
    public class InProcessGenerator : IGenerator
    {
        /// <summary>returns a success</summary>
        public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
            => GenerateResult.Success(null, Array.Empty<string>());
    }
}