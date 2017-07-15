using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>
    /// Implementation of <see cref="IGenerator"/> for in-process benchmarks.
    /// </summary>
    public class InProcessGenerator : IGenerator
    {
        /// <summary>Generates the project for benchmark.</summary>
        /// <param name="benchmark">The benchmark.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="rootArtifactsFolderPath">The root artifacts folder path.</param>
        /// <param name="config">The config for benchmark.</param>
        /// <param name="resolver">The resolver.</param>
        /// <returns>Generation result.</returns>
        public GenerateResult GenerateProject(
            Benchmark benchmark, ILogger logger,
            string rootArtifactsFolderPath, IConfig config,
            IResolver resolver) 
            => GenerateResult.Success(null, Array.Empty<string>());
    }
}