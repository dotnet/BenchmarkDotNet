using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace BenchmarkDotNet.Toolchains.Results
{
    public class ExecuteResult
    {
        public bool FoundExecutable { get; }
        public int? ExitCode { get; }
        public int? ProcessId { get; }
        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<Measurement> Measurements => measurements;

        /// <summary>
        /// All lines printed to standard output by the Benchmark process
        /// </summary>
        public IReadOnlyList<string> StandardOutput { get; }

        /// <summary>
        /// Lines reported by the Benchmark process that are starting with "//"
        /// </summary>
        public IReadOnlyList<string> PrefixedLines { get; }

        /// <summary>
        /// Lines reported by the Benchmark process that are not starting with "//"
        /// </summary>
        public IReadOnlyList<string> Results { get; }

        internal readonly GcStats GcStats;
        private readonly List<string> errors;
        private readonly List<Measurement> measurements;

        // benchmark can fail after few Workload Actual iterations
        // that is why we search for Workload Results as they are produced at the end
        public bool IsSuccess => Measurements.Any(m => m.Is(IterationMode.Workload, IterationStage.Result));

        public ExecuteResult(bool foundExecutable, int? exitCode, int? processId, IReadOnlyList<string> results, IReadOnlyList<string> prefixedLines, IReadOnlyList<string> standardOutput, int launchIndex)
        {
            FoundExecutable = foundExecutable;
            Results = results;
            ProcessId = processId;
            ExitCode = exitCode;
            PrefixedLines = prefixedLines;
            StandardOutput = standardOutput;
            Parse(results, prefixedLines, launchIndex, out measurements, out errors, out GcStats);
        }

        internal ExecuteResult(List<Measurement> measurements, GcStats gcStats)
        {
            FoundExecutable = true;
            ExitCode = 0;
            errors = [];
            PrefixedLines = [];
            this.measurements = measurements;
            GcStats = gcStats;
            StandardOutput = [];
            Results = [];
        }

        internal ExecuteResult(List<Measurement> measurements)
        {
            FoundExecutable = true;
            ExitCode = 0;
            errors = [];
            PrefixedLines = [];
            this.measurements = measurements;
            GcStats = GcStats.Empty;
            StandardOutput = [];
            Results = [];
        }

        internal static ExecuteResult FromRunResults(RunResults runResults, int exitCode)
            => exitCode != 0
                ? CreateFailed(exitCode)
                : new ExecuteResult([.. runResults.GetAllMeasurements()], runResults.GCStats);

        internal static ExecuteResult CreateFailed(int exitCode = -1)
            => new(false, exitCode, default, [], [], [], 0);

        internal static ExecuteResult CreateFailed(string error)
        {
            var result = new ExecuteResult(false, -1, default, [], [], [], 0);
            result.errors.Add(error);
            return result;
        }

        public override string ToString() => "ExecuteResult: " + (FoundExecutable ? "Found executable" : "Executable not found");

        public void LogIssues(ILogger logger, BuildResult buildResult)
        {
            if (!FoundExecutable)
            {
                logger.WriteLineError($"Executable {buildResult.ArtifactsPaths.ExecutablePath} not found");
            }

            // exit code can be different than 0 if the process has hanged at the end
            // so we check if some results were reported, if not then it was a failure
            if (ExitCode != 0 && Results.Count == 0)
            {
                logger.WriteLineError("ExitCode != 0 and no results reported");
            }

            foreach (string error in Errors)
            {
                logger.WriteLineError(error);
            }

            if (!Measurements.Any(m => m.Is(IterationMode.Workload, IterationStage.Result)))
            {
                logger.WriteLineError("No Workload Results were obtained from the run.");
            }
        }

        private static void Parse(IReadOnlyList<string> results, IReadOnlyList<string> prefixedLines, int launchIndex, out List<Measurement> measurements,
            out List<string> errors, out GcStats gcStats)
        {
            measurements = [];
            errors = [];
            gcStats = default;

            foreach (string line in results.Where(text => !string.IsNullOrEmpty(text)))
            {
                Measurement measurement = Measurement.Parse(line, launchIndex);
                if (measurement.IterationMode != IterationMode.Unknown)
                {
                    measurements.Add(measurement);
                }
            }

            foreach (string line in prefixedLines.Where(text => !string.IsNullOrEmpty(text)))
            {
                if (line.StartsWith(ValidationErrorReporter.ConsoleErrorPrefix))
                {
                    errors.Add(line[ValidationErrorReporter.ConsoleErrorPrefix.Length..].Trim());
                }
                else if (line.StartsWith(GcStats.ResultsLinePrefix))
                {
                    gcStats = GcStats.Parse(line);
                }
            }

            if (errors.Count > 0)
            {
                measurements.Clear();
                gcStats = default;
            }
        }
    }
}
