using System;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class GenerateResult
    {
        public string DirectoryPath { get; }
        public string ProgramName { get; }

        public bool IsGenerateSuccess { get; }
        public Exception GenerateException { get; }

        public GenerateResult(string directoryPath, string programName, bool isGenerateSuccess, Exception generateException)
        {
            DirectoryPath = directoryPath;
            ProgramName = programName;
            IsGenerateSuccess = isGenerateSuccess;
            GenerateException = generateException;
        }

        public override string ToString() => "GenerateResult: " + (IsGenerateSuccess ? "Success" : "Fail");
    }
}