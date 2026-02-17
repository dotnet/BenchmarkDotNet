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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    internal class WasmExecutor : IExecutor
    {
        public async ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters)
        {
            string exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;

            if (!File.Exists(exePath))
            {
                return ExecuteResult.CreateFailed();
            }

            return await Execute(executeParameters.BenchmarkCase, executeParameters.BenchmarkId, executeParameters.Logger, executeParameters.BuildResult.ArtifactsPaths,
                executeParameters.Diagnoser, executeParameters.CompositeInProcessDiagnoser, executeParameters.Resolver, executeParameters.LaunchIndex,
                executeParameters.DiagnoserRunMode);
        }

        private static async ValueTask<ExecuteResult> Execute(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, Loggers.ILogger logger, ArtifactsPaths artifactsPaths,
            IDiagnoser? diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser, IResolver resolver, int launchIndex,
            Diagnosers.RunMode diagnoserRunMode)
        {
            using var webSocketListener = new WebSocketListener();
            var port = await webSocketListener.StartAndGetPortAsync();
            try
            {
                using Process process = CreateProcess(benchmarkCase, artifactsPaths, benchmarkId.ToArguments(port, diagnoserRunMode), resolver);
                using ConsoleExitHandler consoleExitHandler = new(process, logger);
                using AsyncProcessOutputReader processOutputReader = new(process, logOutput: true, logger, readStandardError: false);

                diagnoser?.Handle(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId));
                return await Execute(process, benchmarkCase, processOutputReader,
                    benchmarkId, logger, consoleExitHandler, launchIndex, diagnoser,
                    compositeInProcessDiagnoser, webSocketListener);
            }
            finally
            {
                diagnoser?.Handle(HostSignal.AfterProcessExit, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId));
            }
        }

        private static Process CreateProcess(BenchmarkCase benchmarkCase, ArtifactsPaths artifactsPaths, string args, IResolver resolver)
        {
            WasmRuntime runtime = (WasmRuntime)benchmarkCase.GetRuntime();
            const string mainJs = "benchmark-main.mjs";

            var start = new ProcessStartInfo
            {
                FileName = runtime.JavaScriptEngine,
                Arguments = $"{runtime.JavaScriptEngineArguments} {mainJs} -- --run {artifactsPaths.ProgramName}.dll {args} ",
                WorkingDirectory = Path.Combine(artifactsPaths.BinariesDirectoryPath, "wwwroot"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false, // not supported by WASM!
                RedirectStandardError = false, // #1629
                CreateNoWindow = true
            };

            start.SetEnvironmentVariables(benchmarkCase, resolver);

            return new Process() { StartInfo = start };
        }

        private static async ValueTask<ExecuteResult> Execute(Process process, BenchmarkCase benchmarkCase, AsyncProcessOutputReader processOutputReader,
            BenchmarkId benchmarkId, Loggers.ILogger logger, ConsoleExitHandler consoleExitHandler, int launchIndex, IDiagnoser? diagnoser,
            CompositeInProcessDiagnoser compositeInProcessDiagnoser, WebSocketListener webSocketListener)
        {
            List<string> results;
            List<string> prefixedOutput;
            using (Broker broker = new(logger, process, diagnoser, compositeInProcessDiagnoser, benchmarkCase, benchmarkId, webSocketListener))
            {
                logger.WriteLineInfo($"// Execute: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

                diagnoser?.Handle(HostSignal.BeforeProcessStart, broker.DiagnoserActionParameters);

                process.Start();

                diagnoser?.Handle(HostSignal.AfterProcessStart, broker.DiagnoserActionParameters);

                processOutputReader.BeginRead();

                process.EnsureHighPriority(logger);
                if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
                {
                    process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
                }

                await broker.ProcessData();

                results = broker.Results;
                prefixedOutput = broker.PrefixedOutput;
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

            return new ExecuteResult(true,
                process.HasExited ? process.ExitCode : null,
                process.Id,
                results,
                prefixedOutput,
                processOutputReader.GetOutputLines(),
                launchIndex);
        }
    }
}
