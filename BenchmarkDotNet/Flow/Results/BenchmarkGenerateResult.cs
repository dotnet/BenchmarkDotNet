using System;

namespace BenchmarkDotNet.Flow.Results
{
    public class BenchmarkGenerateResult
    {
        public string DirectoryPath { get; }

        public bool IsGenerateSuccess { get; }
        public Exception GenerateException { get; }

        public BenchmarkGenerateResult(string directoryPath, bool isGenerateSuccess, Exception generateException)
        {
            DirectoryPath = directoryPath;
            IsGenerateSuccess = isGenerateSuccess;
            GenerateException = generateException;
        }
    }
}