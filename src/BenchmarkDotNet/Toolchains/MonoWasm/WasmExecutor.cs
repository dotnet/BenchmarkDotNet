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
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    internal class WasmExecutor : IExecutor
    {
        private sealed class ProcessListener(IpcListener listener, Process process) : IDisposable
        {
            public IpcListener Listener { get; } = listener;
            public Process Process { get; } = process;

            public void Dispose()
            {
                Process.Dispose();
                Listener.Dispose();
            }
        }

        public async ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters, CancellationToken cancellationToken)
        {
            string exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;

            if (!File.Exists(exePath))
            {
                return ExecuteResult.CreateFailed();
            }

            return await Execute(executeParameters.BenchmarkCase, executeParameters.BenchmarkId, executeParameters.Logger, executeParameters.BuildResult.ArtifactsPaths,
                executeParameters.Diagnoser, executeParameters.CompositeInProcessDiagnoser, executeParameters.Resolver, executeParameters.LaunchIndex,
                executeParameters.DiagnoserRunMode, cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask<bool> ProbeWebSocketSupportAsync(BenchmarkCase benchmarkCase, ArtifactsPaths artifactsPaths, IResolver resolver, CancellationToken cancellationToken)
        {
            // Check if the JavaScript runtime supports WebSocket
            using var probeProcess = CreateProcess(benchmarkCase, artifactsPaths, "--getSupportsWebSocket", resolver);
            probeProcess.Start();

            string output;
            try
            {
#if NET7_0_OR_GREATER
                output = await probeProcess.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#else
                output = await probeProcess.StandardOutput.ReadToEndAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
#endif
            }
            finally
            {
                if (!probeProcess.WaitForExit(milliseconds: (int) ExecuteParameters.ProcessExitTimeout.TotalMilliseconds))
                {
                    probeProcess.KillTree();
                }
            }

            // Parse output for "supportsWebSocket: true" or "supportsWebSocket: false"
            foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("supportsWebSocket:", StringComparison.Ordinal))
                {
                    string value = line["supportsWebSocket:".Length..].Trim();
                    return bool.TryParse(value, out bool result) && result;
                }
            }

            // Default to file-based IPC if probe fails
            return false;
        }

        private static async ValueTask<ProcessListener> CreateProcessListenerAsync(
            BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ArtifactsPaths artifactsPaths,
            IResolver resolver, Diagnosers.RunMode diagnoserRunMode, CancellationToken cancellationToken)
        {
            WasmRuntime runtime = (WasmRuntime)benchmarkCase.GetRuntime();

            bool useWebSocket = runtime.IpcType == WasmIpcType.Auto
                // Probe the JavaScript runtime to check if it supports WebSocket
                ? await ProbeWebSocketSupportAsync(benchmarkCase, artifactsPaths, resolver, cancellationToken).ConfigureAwait(false)
                : runtime.IpcType == WasmIpcType.WebSocket;

            IpcListener listener;
            string args;

            if (useWebSocket)
            {
                var webSocketListener = new WebSocketListener();
                try
                {
                    var port = await webSocketListener.StartAndGetPortAsync().ConfigureAwait(false);
                    listener = webSocketListener;
                    args = benchmarkId.ToArguments(port, diagnoserRunMode);
                }
                catch
                {
                    // Ensure WebSocketListener is disposed if we fail after creating it
                    webSocketListener.Dispose();
                    throw;
                }
            }
            else
            {
                // File-based IPC for shell engines (v8/d8, SpiderMonkey, etc.)
                var fileStdOutListener = new FileStdOutListener(artifactsPaths.BuildArtifactsDirectoryPath);
                listener = fileStdOutListener;
                args = benchmarkId.ToArguments(fileStdOutListener.GetIpcDirectory(), diagnoserRunMode);
            }

            Process process;
            try
            {
                process = CreateProcess(benchmarkCase, artifactsPaths, args, resolver);
            }
            catch
            {
                // Ensure listener is disposed if process creation fails
                listener.Dispose();
                throw;
            }

            return new ProcessListener(listener, process);
        }

        private static async ValueTask<ExecuteResult> Execute(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, Loggers.ILogger logger, ArtifactsPaths artifactsPaths,
            IDiagnoser diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser, IResolver resolver, int launchIndex,
            Diagnosers.RunMode diagnoserRunMode, CancellationToken cancellationToken)
        {
            using ProcessListener processListener = await CreateProcessListenerAsync(benchmarkCase, benchmarkId, artifactsPaths, resolver, diagnoserRunMode, cancellationToken).ConfigureAwait(true);
            try
            {
                using ProcessCleanupHelper processCleanupHelper = new(processListener.Process, logger);
                bool isFileBasedIpc = processListener.Listener is FileStdOutListener;
                using AsyncProcessOutputReader processOutputReader = new(processListener.Process,
                    stdOutLogger: isFileBasedIpc ? NullLogger.Instance : logger,
                    stdErrLogger: logger,
                    channelStandardOutput: isFileBasedIpc,
                    cacheStandardError: false);

                if (isFileBasedIpc)
                {
                    ((FileStdOutListener) processListener.Listener).AttachProcessOutputReader(processOutputReader);
                }

                await diagnoser.HandleAsync(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(processListener.Process, benchmarkCase, benchmarkId), cancellationToken).ConfigureAwait(true);
                return await Execute(processListener.Process, benchmarkCase, processOutputReader,
                    benchmarkId, logger, processCleanupHelper, launchIndex, diagnoser,
                    compositeInProcessDiagnoser, processListener.Listener, cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                await diagnoser.HandleAsync(HostSignal.AfterProcessExit, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId), cancellationToken).ConfigureAwait(false);
            }
        }

        private static Process CreateProcess(BenchmarkCase benchmarkCase, ArtifactsPaths artifactsPaths, string args, IResolver resolver)
        {
            WasmRuntime runtime = (WasmRuntime)benchmarkCase.GetRuntime();

            var start = new ProcessStartInfo
            {
                FileName = runtime.JavaScriptEngine,
                Arguments = runtime.JavaScriptEngineArgumentFormatter(runtime, artifactsPaths, args),
                WorkingDirectory = Path.Combine(artifactsPaths.BinariesDirectoryPath, "wwwroot"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false, // not supported by WASM!
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            start.SetEnvironmentVariables(benchmarkCase, resolver);

            return new Process() { StartInfo = start };
        }

        private static async ValueTask<ExecuteResult> Execute(Process process, BenchmarkCase benchmarkCase, AsyncProcessOutputReader processOutputReader,
            BenchmarkId benchmarkId, Loggers.ILogger logger, ProcessCleanupHelper processCleanupHelper, int launchIndex, IDiagnoser diagnoser,
            CompositeInProcessDiagnoser compositeInProcessDiagnoser, IpcListener ipcListener, CancellationToken cancellationToken)
        {
            WasmRuntime wasmRuntime = (WasmRuntime) benchmarkCase.GetRuntime();
            List<string> results;
            List<string> prefixedOutput;
            try
            {
                using Broker broker = new(logger, process, diagnoser, compositeInProcessDiagnoser, benchmarkCase, benchmarkId, ipcListener);

                logger.WriteLineInfo($"// Execute: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

                await diagnoser.HandleAsync(HostSignal.BeforeProcessStart, broker.DiagnoserActionParameters, cancellationToken).ConfigureAwait(true);

                process.Start();

                await diagnoser.HandleAsync(HostSignal.AfterProcessStart, broker.DiagnoserActionParameters, cancellationToken).ConfigureAwait(true);

                processOutputReader.BeginRead();

                process.EnsureHighPriority(logger);
                if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
                {
                    process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
                }

#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
                await broker.ProcessData(cancellationToken)
                    .AsTask()
                    .WaitAsync(TimeSpan.FromMinutes(wasmRuntime.ProcessTimeoutMinutes)).ConfigureAwait(false);
#pragma warning restore CA2016 // Forward the 'CancellationToken' parameter to methods

                results = broker.Results;
                prefixedOutput = broker.PrefixedOutput;
            }
            finally
            {
                if (!process.WaitForExit(milliseconds: (int) ExecuteParameters.ProcessExitTimeout.TotalMilliseconds))
                {
                    logger.WriteLineInfo($"// The benchmarking process did not quit within {ExecuteParameters.ProcessExitTimeout.TotalSeconds} seconds, it's going to get force killed now.");

                    processOutputReader.CancelRead();
                    processCleanupHelper.KillProcessTree();
                }
                else
                {
                    await processOutputReader.StopReadAsync().ConfigureAwait(false);
                }
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
