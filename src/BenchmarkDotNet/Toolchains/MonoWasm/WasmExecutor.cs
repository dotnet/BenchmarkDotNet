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
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    internal class WasmExecutor : IExecutor
    {
        public ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters)
        {
            string exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;

            var executeResult = !File.Exists(exePath)
                ? ExecuteResult.CreateFailed()
                : Execute(executeParameters.BenchmarkCase, executeParameters.BenchmarkId, executeParameters.Logger, executeParameters.BuildResult.ArtifactsPaths,
                executeParameters.Diagnoser, executeParameters.CompositeInProcessDiagnoser, executeParameters.Resolver, executeParameters.LaunchIndex,
                executeParameters.DiagnoserRunMode);
            return new ValueTask<ExecuteResult>(executeResult);
        }

        private static ExecuteResult Execute(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, ArtifactsPaths artifactsPaths,
            IDiagnoser? diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser, IResolver resolver, int launchIndex,
            Diagnosers.RunMode diagnoserRunMode)
        {
            try
            {
                using Process process = CreateProcess(benchmarkCase, artifactsPaths, benchmarkId.ToArguments(diagnoserRunMode), resolver);
                using ConsoleExitHandler consoleExitHandler = new(process, logger);
                using AsyncProcessOutputReader processOutputReader = new(process, logOutput: true, logger, readStandardError: false);

                diagnoser?.Handle(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId));
                return Execute(process, benchmarkCase, processOutputReader, logger, consoleExitHandler, launchIndex, compositeInProcessDiagnoser);
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
                WorkingDirectory = Path.Combine(artifactsPaths.BinariesDirectoryPath, "AppBundle"),
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
            ILogger logger, ConsoleExitHandler consoleExitHandler, int launchIndex, CompositeInProcessDiagnoser compositeInProcessDiagnoser)
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
            var prefixedLines = new List<string>();
            var resultLines = new List<string>();
            var outputEnumerator = outputLines.GetEnumerator();
            while (outputEnumerator.MoveNext())
            {
                var line = outputEnumerator.Current;
                if (!line.StartsWith("//"))
                {
                    resultLines.Add(line);
                    continue;
                }

                prefixedLines.Add(line);

                // Keep in sync with Broker and InProcessHost.
                if (line.StartsWith(CompositeInProcessDiagnoser.HeaderKey))
                {
                    // Something like "// InProcessDiagnoser 0 1"
                    string[] lineItems = line.Split(' ');
                    int diagnoserIndex = int.Parse(lineItems[2]);
                    int resultsLinesCount = int.Parse(lineItems[3]);
                    var resultsStringBuilder = new StringBuilder();
                    for (int i = 0; i < resultsLinesCount;)
                    {
                        // Strip the prepended "// InProcessDiagnoserResults ".
                        bool movedNext = outputEnumerator.MoveNext();
                        Debug.Assert(movedNext);
                        line = outputEnumerator.Current.Substring(CompositeInProcessDiagnoser.ResultsKey.Length + 1);
                        resultsStringBuilder.Append(line);
                        if (++i < resultsLinesCount)
                        {
                            resultsStringBuilder.AppendLine();
                        }
                    }
                    compositeInProcessDiagnoser.DeserializeResults(diagnoserIndex, benchmarkCase, resultsStringBuilder.ToString());
                }
            }

            return new ExecuteResult(true,
                process.HasExited ? process.ExitCode : null,
                process.Id,
                [.. resultLines],
                [.. prefixedLines],
                outputLines,
                launchIndex);
        }
    }
}
