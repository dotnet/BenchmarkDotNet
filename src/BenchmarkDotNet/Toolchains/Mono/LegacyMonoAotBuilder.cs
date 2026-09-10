using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Mono
{
    internal sealed class LegacyMonoAotBuilder(MonoAotSettings settings) : IBuilder
    {
        public async ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
        {
            var result = await Roslyn.RoslynBuilder.Instance.BuildAsync(generateResult, buildPartition, logger, cancellationToken).ConfigureAwait(false);

            if (!result.IsBuildSuccess)
                return result;

            var exePath = generateResult.ArtifactsPaths.ExecutablePath;
            var environmentVariables = settings.MonoBclPath is null
                ? null
                : new Dictionary<string, string> { { "MONO_PATH", settings.MonoBclPath.FullName } };

            var (exitCode, output) = await ProcessHelper.RunAndReadOutputLineByLineAsync(
                fileName: settings.MonoPath?.FullName ?? "mono",
                arguments: $"{settings.AotArgs} \"{Path.GetFullPath(exePath)}\"",
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
