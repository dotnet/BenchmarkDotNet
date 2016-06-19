using System;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class GenerateResult
    {
        public ArtifactsPaths ArtifactsPaths { get; }
        public bool IsGenerateSuccess { get; }
        public Exception GenerateException { get; }

        public GenerateResult(ArtifactsPaths artifactsPaths, bool isGenerateSuccess, Exception generateException)
        {
            ArtifactsPaths = artifactsPaths;
            IsGenerateSuccess = isGenerateSuccess;
            GenerateException = generateException;
        }

        public static GenerateResult Success(ArtifactsPaths artifactsPaths) => new GenerateResult(artifactsPaths, true, null);

        public static GenerateResult Failure(ArtifactsPaths artifactsPaths, Exception exception = null) => new GenerateResult(artifactsPaths, false, exception);

        public override string ToString() => "GenerateResult: " + (IsGenerateSuccess ? "Success" : "Fail");
    }
}