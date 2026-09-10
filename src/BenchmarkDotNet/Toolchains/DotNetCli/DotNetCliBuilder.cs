using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    // The toolchain resolves the target framework moniker against the runtime before constructing the builder.
    public class DotNetCliBuilder(DotNetCliSettings settings, bool logOutput = false) : IBuilder
    {
        internal FileInfo? CustomDotNetCliPath { get; } = settings.CliPath;

        public async ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
        {
            var buildResult = await new DotNetCliCommand(
                CustomDotNetCliPath,
                generateResult.ArtifactsPaths.ProjectFilePath,
                settings.TargetFrameworkMoniker,
                string.Empty,
                generateResult,
                logger,
                buildPartition,
                [],
                buildPartition.Timeout,
                logOutput: logOutput
            )
                .RestoreThenBuildAsync(cancellationToken)
                .ConfigureAwait(false);
            if (buildResult.IsBuildSuccess &&
                buildPartition.RepresentativeBenchmarkCase.Job.Environment.LargeAddressAware)
            {
                LargeAddressAware.SetLargeAddressAware(generateResult.ArtifactsPaths.ExecutablePath);
            }
            return buildResult;
        }
    }
}
