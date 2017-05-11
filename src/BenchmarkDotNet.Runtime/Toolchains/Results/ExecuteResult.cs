using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class ExecuteResult
    {
        public bool FoundExecutable { get; }
        public int ExitCode { get; }
        public IReadOnlyList<string> Data { get; }
        public IReadOnlyList<string> ExtraOutput { get; }

        public ExecuteResult(bool foundExecutable, int exitCode, IReadOnlyList<string> data, IReadOnlyList<string> linesWithExtraOutput)
        {
            FoundExecutable = foundExecutable;
            Data = data;
            ExitCode = exitCode;
            ExtraOutput = linesWithExtraOutput;
        }

        public override string ToString() => "ExecuteResult: " + (FoundExecutable ? "Found executable" : "Executable not found");
    }
}