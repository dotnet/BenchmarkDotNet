using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchain.Results
{
    public class BenchmarkExecResult
    {
        public bool FoundExecutable { get; }
        public IList<string> Data { get; }

        public BenchmarkExecResult(bool foundExecutable, IList<string> data)
        {
            FoundExecutable = foundExecutable;
            Data = data;
        }
    }
}