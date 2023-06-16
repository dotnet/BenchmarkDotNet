using System;

using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /// <summary>
    /// Implementation of <see cref="IGenerator"/> for in-process (no emit) toolchain.
    /// </summary>
    public class InProcessNoEmitGenerator : IGenerator
    {
        /// <summary>returns a success</summary>
        public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
            => GenerateResult.Success(null, Array.Empty<string>());
    }
}