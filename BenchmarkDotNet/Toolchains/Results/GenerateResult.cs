using System;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class GenerateResult
    {
        public string DirectoryPath { get; }

        public bool IsGenerateSuccess { get; }
        public Exception GenerateException { get; }

        public GenerateResult(string directoryPath, bool isGenerateSuccess, Exception generateException)
        {
            DirectoryPath = directoryPath;
            IsGenerateSuccess = isGenerateSuccess;
            GenerateException = generateException;
        }

        public override string ToString() => "GenerateResult: " + (IsGenerateSuccess ? "Success" : "Fail");
    }
}