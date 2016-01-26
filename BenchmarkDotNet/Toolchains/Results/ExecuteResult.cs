using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class ExecuteResult
    {
        public bool FoundExecutable { get; }
        public IList<string> Data { get; }

        public ExecuteResult(bool foundExecutable, IList<string> data)
        {
            FoundExecutable = foundExecutable;
            Data = data;
        }

        public override string ToString() => "ExecuteResult: " + (FoundExecutable ? "Found executable" : "Executable not found");
    }
}