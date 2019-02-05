using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Toolchains.Results
{
    public class ExecuteResult
    {
        public bool FoundExecutable { get; }
        public int ExitCode { get; }
        public ImmutableArray<string> Data { get; }
        public ImmutableArray<string> ExtraOutput { get; }

        private ExecuteResult(bool foundExecutable, int exitCode, ImmutableArray<string> data, ImmutableArray<string> extraOutput)
        {
            FoundExecutable = foundExecutable;
            Data = data;
            ExitCode = exitCode;
            ExtraOutput = extraOutput;
        }

        public override string ToString() 
            => "ExecuteResult: " + (FoundExecutable ? "Found executable" : "Executable not found");

        public static ExecuteResult CreateExecutableNotFound()
            => new ExecuteResult(false, -1, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);

        public static ExecuteResult CreateEmptyOk()
            => new ExecuteResult(true, 0, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);

        public static ExecuteResult FromExitCode(int processExitCode, ImmutableArray<string> data, ImmutableArray<string> extraOutput)
            => new ExecuteResult(true, processExitCode, data, extraOutput);

        public static ExecuteResult FromRunResults(RunResults runResults, int exitCode)
        {
            if (exitCode != 0)
                return FromExitCode(exitCode, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);

            var builder = ImmutableArray.CreateBuilder<string>();
            foreach (var measurementLine in runResults.GetMeasurements().Select(measurement => measurement.ToOutputLine()))
                builder.Add(measurementLine);
            builder.Add(runResults.GCStats.ToOutputLine());

            return FromExitCode(0, builder.ToImmutable(), ImmutableArray<string>.Empty);
        }
    }
}