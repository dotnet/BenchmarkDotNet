using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    public abstract class OutOfProcessExecutor : IExecutor
    {
        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            if (!File.Exists(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath))
                return ExecuteResult.CreateExecutableNotFound();

            using (var process = new Process { StartInfo = CreateStartInfo(executeParameters) })
                return Execute(process, executeParameters);
        }

        protected abstract (string fileName, string arguments) GetProcessStartArguments(ExecuteParameters parameters);

        protected virtual ImmutableArray<EnvironmentVariable> GetImplicitEnvironmentVariables(ExecuteParameters executeParameters) => ImmutableArray<EnvironmentVariable>.Empty;

        private ProcessStartInfo CreateStartInfo(ExecuteParameters executeParameters)
        {
            var (fileName, arguments) = GetProcessStartArguments(executeParameters);
            var implicitEnvironmentVariables = GetImplicitEnvironmentVariables(executeParameters);

            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = executeParameters.BuildResult.ArtifactsPaths.BinariesDirectoryPath,
                FileName = fileName,
                Arguments = arguments
            };

            foreach (var environmentVariable in implicitEnvironmentVariables)
                start.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;

            start.SetEnvironmentVariables(executeParameters.BenchmarkCase, executeParameters.Resolver);

            return start;
        }

        private static ExecuteResult Execute(Process process, ExecuteParameters executeParameters)
        {
            var reader = new ProcessOutputReader(process, executeParameters);

            try
            {
                ConsoleExitHandler.Instance.Logger = executeParameters.Logger;
                ConsoleExitHandler.Instance.Process = process;

                executeParameters.Logger.WriteLineInfo($"// Start: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");
                executeParameters.Diagnoser?.Handle(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, executeParameters));

                var clock = Chronometer.Start();

                process.Start();

                SetPriorityAndAffinity(process, executeParameters);

                // for scenarios we don't want to read the output - it could affect the timing results
                if (executeParameters.BenchmarkCase.Descriptor.Kind != BenchmarkKind.Scenario)
                    reader.ReadToEnd();

                process.WaitForExit(); // should we add timeout here?

                var timeSpan = clock.GetElapsed();

                if (executeParameters.BenchmarkCase.Descriptor.Kind != BenchmarkKind.Scenario)
                    return ExecuteResult.FromExitCode(process.ExitCode, reader.GetLinesWithResults(), reader.GetLinesWithExtraOutput());

                // we don't have the Results printed to the output and we don't read to output
                // so we just add a "fake" line for the total execution time
                var totalExecutionTime = new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, timeSpan.GetNanoseconds());
                executeParameters.Logger.WriteLine(totalExecutionTime.ToOutputLine()); // we print it just to show the users the execution time
                return ExecuteResult.FromExitCode(process.ExitCode, ImmutableArray.Create(totalExecutionTime.ToOutputLine()), ImmutableArray<string>.Empty);
            }
            finally
            {
                ConsoleExitHandler.Instance.Process = null;
                ConsoleExitHandler.Instance.Logger = null;

                // whether we fail or succeed we must let the diagnosers know!! MUST HAVE!!
                executeParameters.Diagnoser?.Handle(HostSignal.AfterProcessExit, new DiagnoserActionParameters(process, executeParameters));
            }
        }

        private static void SetPriorityAndAffinity(Process process, ExecuteParameters executeParameters)
        {
            process.TryEnsureHighPriority(executeParameters.Logger);

            if (executeParameters.BenchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
                process.TrySetAffinity(executeParameters.BenchmarkCase.Job.Environment.Affinity, executeParameters.Logger);
        }
    }
}