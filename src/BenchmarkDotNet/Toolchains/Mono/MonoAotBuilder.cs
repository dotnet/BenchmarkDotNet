using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
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
        public async ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
        {
            var result = await Roslyn.Builder.Instance.BuildAsync(generateResult, buildPartition, logger, cancellationToken).ConfigureAwait(false);

            if (!result.IsBuildSuccess)
                return result;

            var exePath = generateResult.ArtifactsPaths.ExecutablePath;
            var monoRuntime = (MonoRuntime)buildPartition.Runtime;
            var environmentVariables = monoRuntime.MonoBclPath.IsBlank()
                ? null
                : new Dictionary<string, string> { { "MONO_PATH", monoRuntime.MonoBclPath } };

            var (exitCode, output) = await ProcessHelper.RunAndReadOutputLineByLineAsync(
                fileName: monoRuntime.CustomPath.IsNotBlank() ? monoRuntime.CustomPath : "mono",
                arguments: $"{monoRuntime.AotArgs} \"{Path.GetFullPath(exePath)}\"",
                workingDirectory: Path.GetDirectoryName(exePath)!,
                environmentVariables: environmentVariables,
                includeErrors: true,
                logger: logger,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            return exitCode != 0
                ? BuildResult.Failure(generateResult, $"Attempt to AOT failed: with exit code: {exitCode}, output: {string.Join(Environment.NewLine, output)}")
                : result;
        }
    }
}