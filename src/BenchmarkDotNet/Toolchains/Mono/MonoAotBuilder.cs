using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Mono
{
    [PublicAPI]
    public class MonoAotBuilder : IBuilder
    {
        [PublicAPI]
        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var result = Roslyn.Builder.Instance.Build(generateResult, buildPartition, logger);

            if (!result.IsBuildSuccess)
                return result;

            var exePath = generateResult.ArtifactsPaths.ExecutablePath;
            var monoRuntime = (MonoRuntime)buildPartition.Runtime;
            var environmentVariables = string.IsNullOrEmpty(monoRuntime.MonoBclPath)
                ? null
                : new Dictionary<string, string> { { "MONO_PATH", monoRuntime.MonoBclPath } };

            var (exitCode, output) = ProcessHelper.RunAndReadOutputLineByLine(
                fileName: monoRuntime.CustomPath ?? "mono",
                arguments: $"{monoRuntime.AotArgs} \"{Path.GetFullPath(exePath)}\"",
                workingDirectory: Path.GetDirectoryName(exePath),
                environmentVariables: environmentVariables,
                includeErrors: true,
                logger: logger);

            return exitCode != 0
                ? BuildResult.Failure(generateResult, $"Attempt to AOT failed: with exit code: {exitCode}, output: {string.Join(Environment.NewLine, output)}")
                : result;
        }
    }
}