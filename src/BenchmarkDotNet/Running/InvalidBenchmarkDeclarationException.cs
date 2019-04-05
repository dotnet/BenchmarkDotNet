using System;

namespace BenchmarkDotNet.Running
{
    public class InvalidBenchmarkDeclarationException : Exception
    {
        public InvalidBenchmarkDeclarationException(string message) : base(message) { }
    }
}