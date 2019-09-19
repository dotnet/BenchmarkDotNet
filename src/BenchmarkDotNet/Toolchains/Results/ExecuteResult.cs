using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class ExecuteResult
    {
        public bool FoundExecutable { get; }
        public int ExitCode { get; }
        public int? ProcessId { get; }
        public IReadOnlyList<string> Data { get; }
        public IReadOnlyList<string> ExtraOutput { get; }

        public ExecuteResult(bool foundExecutable, int exitCode, int? processId, IReadOnlyList<string> data, IReadOnlyList<string> linesWithExtraOutput)
        {
            FoundExecutable = foundExecutable;
            Data = data;
            ProcessId = processId;
            ExitCode = exitCode;
            ExtraOutput = linesWithExtraOutput;
        }

        public override string ToString() => "ExecuteResult: " + (FoundExecutable ? "Found executable" : "Executable not found");
    }
}