using System;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Results
{
    [PublicAPI]
    public class BuildResult : GenerateResult
    {
        public bool IsBuildSuccess { get; }
        public string ErrorMessage { get; }
        public string ExecutablePath { get; }

        private BuildResult(GenerateResult generateResult, bool isBuildSuccess, string executablePath, string errorMessage)
            : base(generateResult.ArtifactsPaths, generateResult.IsGenerateSuccess, generateResult.GenerateException, generateResult.ArtifactsToCleanup)
        {
            IsBuildSuccess = isBuildSuccess;
            ExecutablePath = executablePath;
            ErrorMessage = errorMessage;
        }

        [PublicAPI]
        public static BuildResult Success(GenerateResult generateResult, string executablePath)
            => new BuildResult(generateResult, true, executablePath, null);

        [PublicAPI]
        public static BuildResult Failure(GenerateResult generateResult, string errorMessage)
            => new BuildResult(generateResult, false, null, errorMessage);

        [PublicAPI]
        public static BuildResult Failure(GenerateResult generateResult, Exception exception)
            => new BuildResult(generateResult, false, null, $"Exception! {Environment.NewLine}Message: {exception.Message},{Environment.NewLine}Stack trace:{Environment.NewLine}{exception.StackTrace}");

        public override string ToString() => "BuildResult: " + (IsBuildSuccess ? "Success" : "Failure");

        internal bool TryToExplainFailureReason(out string reason) => MsBuildErrorMapper.TryToExplainFailureReason(this, out reason);
    }
}