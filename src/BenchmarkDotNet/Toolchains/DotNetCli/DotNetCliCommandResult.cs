using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    public struct DotNetCliCommandResult
    {
        [PublicAPI] public bool IsSuccess { get; }

        [PublicAPI] public TimeSpan ExecutionTime { get; }

        [PublicAPI] public string StandardOutput { get; }

        [PublicAPI] public string StandardError { get; }

        public string AllInformation => $"Standard output: {Environment.NewLine} {StandardOutput} {Environment.NewLine} Standard error: {Environment.NewLine} {StandardError}";

        [PublicAPI] public bool HasNonEmptyErrorMessage => !string.IsNullOrEmpty(StandardError);

        private DotNetCliCommandResult(bool isSuccess, TimeSpan executionTime, string standardOutput, string standardError)
        {
            IsSuccess = isSuccess;
            ExecutionTime = executionTime;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        public static DotNetCliCommandResult Success(TimeSpan time, string standardOutput)
            => new DotNetCliCommandResult(true, time, standardOutput, string.Empty);

        public static DotNetCliCommandResult Failure(TimeSpan time, string standardError, string standardOutput)
            => new DotNetCliCommandResult(false, time, standardOutput, standardError);

        [PublicAPI]
        public BuildResult ToBuildResult(ArtifactsPaths artifactsPaths)
            => IsSuccess
                ? BuildResult.Success(artifactsPaths)
                : BuildResult.Failure(artifactsPaths, AllInformation);
    }
}