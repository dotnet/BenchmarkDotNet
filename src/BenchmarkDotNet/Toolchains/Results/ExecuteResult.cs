using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class ExecuteResult
    {
        public int ExitCode { get; }
        public int? ProcessId { get; }
        public IReadOnlyList<string> Data { get; }
        public IReadOnlyList<string> ExtraOutput { get; }

        public ExecuteResult(int exitCode, int? processId, IReadOnlyList<string> data, IReadOnlyList<string> linesWithExtraOutput)
        {
            Data = data;
            ProcessId = processId;
            ExitCode = exitCode;
            ExtraOutput = linesWithExtraOutput;
        }
    }
}