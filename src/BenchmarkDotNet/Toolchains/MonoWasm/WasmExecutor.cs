using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    internal class WasmExecutor : IExecutor
    {
        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            string exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;

            if (!File.Exists(exePath))
            {
                return ExecuteResult.CreateFailed();
            }

            return Execute(executeParameters.BenchmarkCase, executeParameters.BenchmarkId, executeParameters.Logger, executeParameters.BuildResult.ArtifactsPaths,
                executeParameters.Diagnoser, executeParameters.Resolver, executeParameters.LaunchIndex);
        }

        private static ExecuteResult Execute(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, ArtifactsPaths artifactsPaths,
            IDiagnoser diagnoser, IResolver resolver, int launchIndex)
        {
            try
            {
                using (Process process = CreateProcess(benchmarkCase, artifactsPaths, benchmarkId.ToArguments(), resolver))
                using (ConsoleExitHandler consoleExitHandler = new (process, logger))
                using (AsyncProcessOutputReader processOutputReader = new (process, logOutput: true, logger, readStandardError: false))
                {
                    diagnoser?.Handle(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId));

                    return Execute(process, benchmarkCase, processOutputReader, logger, consoleExitHandler, launchIndex);
                }
            }
            finally
            {
                diagnoser?.Handle(HostSignal.AfterProcessExit, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId));
            }
        }

        private static Process CreateProcess(BenchmarkCase benchmarkCase, ArtifactsPaths artifactsPaths, string args, IResolver resolver)
        {
            WasmRuntime runtime = (WasmRuntime)benchmarkCase.GetRuntime();
            string mainJs = runtime.RuntimeMoniker < RuntimeMoniker.WasmNet70 ? "main.js" : "test-main.js";

            var start = new ProcessStartInfo
            {
                FileName = runtime.JavaScriptEngine,
                Arguments = $"{runtime.JavaScriptEngineArguments} {mainJs} -- --run {artifactsPaths.ProgramName}.dll {args} ",
                WorkingDirectory = artifactsPaths.BinariesDirectoryPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false, // not supported by WASM!
                RedirectStandardError = false, // #1629
                CreateNoWindow = true
            };

            start.SetEnvironmentVariables(benchmarkCase, resolver);

            return new Process() { StartInfo = start };
        }

        private static ExecuteResult Execute(Process process, BenchmarkCase benchmarkCase, AsyncProcessOutputReader processOutputReader,
            ILogger logger, ConsoleExitHandler consoleExitHandler, int launchIndex)
        {
            logger.WriteLineInfo($"// Execute: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

            process.Start();
            processOutputReader.BeginRead();

            process.EnsureHighPriority(logger);
            if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
            {
                process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
            }

            if (!process.WaitForExit(milliseconds: (int)TimeSpan.FromMinutes(10).TotalMilliseconds))
            {
                logger.WriteLineInfo("// The benchmarking process did not finish within 10 minutes, it's going to get force killed now.");

                processOutputReader.CancelRead();
                consoleExitHandler.KillProcessTree();
            }
            else
            {
                processOutputReader.StopRead();
            }

            ImmutableArray<string> outputLines = processOutputReader.GetOutputLines();

            return new ExecuteResult(true,
                process.HasExited ? process.ExitCode : null,
                process.Id,
                outputLines.Where(line => !line.StartsWith("//")).ToArray(),
                outputLines.Where(line => line.StartsWith("//")).ToArray(),
                outputLines,
                launchIndex);
        }
    }
}
