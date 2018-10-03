using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Results
{
    [PublicAPI]
    public class BuildResult : GenerateResult
    {
        public bool IsBuildSuccess { get; }
        public Exception BuildException { get; }

        private BuildResult(GenerateResult generateResult, bool isBuildSuccess, Exception buildException)
            : base(generateResult.ArtifactsPaths, generateResult.IsGenerateSuccess, generateResult.GenerateException, generateResult.ArtifactsToCleanup)
        {
            IsBuildSuccess = isBuildSuccess;
            BuildException = buildException;
        }

        public bool FailedToAccess => IsGenerateSuccess && !IsBuildSuccess && BuildException.Message.Contains("cannot access");

        [PublicAPI] public static BuildResult Success(GenerateResult generateResult) => new BuildResult(generateResult, true, null);

        [PublicAPI] public static BuildResult Failure(GenerateResult generateResult, Exception exception = null) => new BuildResult(generateResult, false, exception);

        public override string ToString() => "BuildResult: " + (IsBuildSuccess ? "Success" : "Failure");
    }
}