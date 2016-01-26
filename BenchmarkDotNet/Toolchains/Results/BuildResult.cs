using System;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class BuildResult : GenerateResult
    {
        public bool IsBuildSuccess { get; }
        public Exception BuildException { get; }

        public BuildResult(GenerateResult generateResult, bool isBuildSuccess, Exception buildException) :
            base(generateResult.DirectoryPath, generateResult.IsGenerateSuccess, generateResult.GenerateException)
        {
            IsBuildSuccess = isBuildSuccess;
            BuildException = buildException;
        }
    }
}