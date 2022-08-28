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

        private BuildResult(GenerateResult generateResult, bool isBuildSuccess, string errorMessage)
            : base(generateResult.ArtifactsPaths, generateResult.IsGenerateSuccess, generateResult.GenerateException, generateResult.ArtifactsToCleanup)
        {
            IsBuildSuccess = isBuildSuccess;
            ErrorMessage = errorMessage;
        }

        [PublicAPI]
        public static BuildResult Success(GenerateResult generateResult)
            => new BuildResult(generateResult, true, null);

        [PublicAPI]
        public static BuildResult Failure(GenerateResult generateResult, string errorMessage)
            => new BuildResult(generateResult, false, errorMessage);

        [PublicAPI]
        public static BuildResult Failure(GenerateResult generateResult, Exception exception)
            => new BuildResult(generateResult, false, $"Exception! {Environment.NewLine}Message: {exception.Message},{Environment.NewLine}Stack trace:{Environment.NewLine}{exception.StackTrace}");

        public override string ToString() => "BuildResult: " + (IsBuildSuccess ? "Success" : "Failure");

        internal bool TryToExplainFailureReason(out string reason) => MsBuildErrorMapper.TryToExplainFailureReason(this, out reason);
    }
}