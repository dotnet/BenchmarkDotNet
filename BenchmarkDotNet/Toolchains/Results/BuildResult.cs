using System;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class BuildResult : GenerateResult
    {
        public bool IsBuildSuccess { get; }
        public Exception BuildException { get; }

        public BuildResult(GenerateResult generateResult, bool isBuildSuccess, Exception buildException)
            : base(generateResult.ArtifactsPaths, generateResult.IsGenerateSuccess, generateResult.GenerateException)
        {
            IsBuildSuccess = isBuildSuccess;
            BuildException = buildException;
        }

        public static BuildResult Success(GenerateResult generateResult) => new BuildResult(generateResult, true, null);

        public static BuildResult Failure(GenerateResult generateResult, Exception exception = null) => new BuildResult(generateResult, false, exception);

        public override string ToString() => "BuildResult: " + (IsBuildSuccess ? "Success" : "Fail");
    }
}