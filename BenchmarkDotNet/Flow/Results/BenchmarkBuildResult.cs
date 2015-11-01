using System;

namespace BenchmarkDotNet.Flow.Results
{
    public class BenchmarkBuildResult : BenchmarkGenerateResult
    {
        public bool IsBuildSuccess { get; }
        public Exception BuildException { get; }

        public BenchmarkBuildResult(BenchmarkGenerateResult generateResult, bool isBuildSuccess, Exception buildException) :
            base(generateResult.DirectoryPath, generateResult.IsGenerateSuccess, generateResult.GenerateException)
        {
            IsBuildSuccess = isBuildSuccess;
            BuildException = buildException;
        }
    }
}