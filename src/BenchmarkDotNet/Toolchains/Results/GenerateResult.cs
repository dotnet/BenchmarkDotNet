using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class GenerateResult
    {
        public ArtifactsPaths ArtifactsPaths { get; }
        public bool IsGenerateSuccess { get; }
        public Exception GenerateException { get; }
        public IReadOnlyCollection<string> ArtifactsToCleanup { get; }

        public GenerateResult(ArtifactsPaths artifactsPaths, bool isGenerateSuccess, Exception generateException,
            IReadOnlyCollection<string> artifactsToCleanup)
        {
            ArtifactsPaths = artifactsPaths;
            IsGenerateSuccess = isGenerateSuccess;
            GenerateException = generateException;
            ArtifactsToCleanup = artifactsToCleanup;
        }

        public static GenerateResult Success(ArtifactsPaths artifactsPaths, IReadOnlyCollection<string> artifactsToCleanup)
            => new GenerateResult(artifactsPaths, true, null, artifactsToCleanup);

        public static GenerateResult Failure(ArtifactsPaths artifactsPaths, IReadOnlyCollection<string> artifactsToCleanup, Exception exception = null)
            => new GenerateResult(artifactsPaths, false, exception, artifactsToCleanup);

        public override string ToString() => "GenerateResult: " + (IsGenerateSuccess ? "Success" : "Fail");
    }
}