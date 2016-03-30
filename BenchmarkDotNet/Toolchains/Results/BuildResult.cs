using System;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class BuildResult : GenerateResult
    {
        public bool IsBuildSuccess { get; }
        public Exception BuildException { get; }
        public string ExecutablePath { get; }

        public BuildResult(GenerateResult generateResult, bool isBuildSuccess, Exception buildException, string executablePath) 
            : base(generateResult.DirectoryPath, generateResult.IsGenerateSuccess, generateResult.GenerateException)
        {
            IsBuildSuccess = isBuildSuccess;
            BuildException = buildException;
            ExecutablePath = executablePath;
        }

        public override string ToString() => "BuildResult: "  + (IsBuildSuccess ? "Success" : "Fail");
    }
}