using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public class DotNetCliExecutor : IExecutor
    {
        public DotNetCliExecutor(string customDotNetCliPath) => CustomDotNetCliPath = customDotNetCliPath;

        private string CustomDotNetCliPath { get; }

        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            if (!File.Exists(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath))
            {
                executeParameters.Logger.WriteLineError($"Did not find {executeParameters.BuildResult.ArtifactsPaths.ExecutablePath}, but the folder contained:");
                foreach (var file in new DirectoryInfo(executeParameters.BuildResult.ArtifactsPaths.BinariesDirectoryPath).GetFiles("*.*"))
                    executeParameters.Logger.WriteLineError(file.Name);

                return ExecuteResult.CreateFailed();
            }

            try
            {
                return Execute(
                    executeParameters.BenchmarkCase,
                    executeParameters.BenchmarkId,
                    executeParameters.Logger,
                    executeParameters.BuildResult.ArtifactsPaths,
                    executeParameters.Diagnoser,
                    Path.GetFileName(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath),
                    executeParameters.Resolver,
                    executeParameters.LaunchIndex);
            }
            finally
            {
                executeParameters.Diagnoser?.Handle(
                    HostSignal.AfterProcessExit,
                    new DiagnoserActionParameters(null, executeParameters.BenchmarkCase, executeParameters.BenchmarkId));
            }
        }

        private ExecuteResult Execute(BenchmarkCase benchmarkCase,
                                      BenchmarkId benchmarkId,
                                      ILogger logger,
                                      ArtifactsPaths artifactsPaths,
                                      IDiagnoser diagnoser,
                                      string executableName,
                                      IResolver resolver,
                                      int launchIndex)
        {
            using AnonymousPipeServerStream inputFromBenchmark = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            using AnonymousPipeServerStream acknowledgments = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);

            var startInfo = DotNetCliCommandExecutor.BuildStartInfo(
                CustomDotNetCliPath,
                artifactsPaths.BinariesDirectoryPath,
                $"{executableName.EscapeCommandLine()} {benchmarkId.ToArguments(inputFromBenchmark.GetClientHandleAsString(), acknowledgments.GetClientHandleAsString())}",
                redirectStandardOutput: true,
                redirectStandardInput: false,
                redirectStandardError: false); // #1629

            startInfo.SetEnvironmentVariables(benchmarkCase, resolver);

            using (Process process = new () { StartInfo = startInfo })
            using (ConsoleExitHandler consoleExitHandler = new (process, logger))
            using (AsyncProcessOutputReader processOutputReader = new (process, logOutput: true, logger, readStandardError: false))
            {
                Broker broker = new (logger, process, diagnoser, benchmarkCase, benchmarkId, inputFromBenchmark, acknowledgments);

                logger.WriteLineInfo($"// Execute: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

                diagnoser?.Handle(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId));

                process.Start();
                processOutputReader.BeginRead();

                process.EnsureHighPriority(logger);
                if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
                {
                    process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
                }

                broker.ProcessData();

                if (!process.WaitForExit(milliseconds: (int)ExecuteParameters.ProcessExitTimeout.TotalMilliseconds))
                {
                    logger.WriteLineInfo($"// The benchmarking process did not quit within {ExecuteParameters.ProcessExitTimeout.TotalSeconds} seconds, it's going to get force killed now.");

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
                    broker.Results,
                    broker.PrefixedOutput,
                    processOutputReader.GetOutputLines(),
                    launchIndex);
            }
        }
    }
}
