using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
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
    public class DotNetCliExecutor(string customDotNetCliPath) : IExecutor
    {
        public ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters)
        {
            if (!File.Exists(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath))
            {
                executeParameters.Logger.WriteLineError($"Did not find {executeParameters.BuildResult.ArtifactsPaths.ExecutablePath}, but the folder contained:");
                foreach (var file in new DirectoryInfo(executeParameters.BuildResult.ArtifactsPaths.BinariesDirectoryPath).GetFiles("*.*"))
                {
                    executeParameters.Logger.WriteLineError(file.Name);
                }

                return new ValueTask<ExecuteResult>(ExecuteResult.CreateFailed());
            }

            try
            {
                var executeResult = Execute(
                    executeParameters.BenchmarkCase,
                    executeParameters.BenchmarkId,
                    executeParameters.Logger,
                    executeParameters.BuildResult.ArtifactsPaths,
                    executeParameters.Diagnoser,
                    executeParameters.CompositeInProcessDiagnoser,
                    Path.GetFileName(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath),
                    executeParameters.Resolver,
                    executeParameters.LaunchIndex,
                    executeParameters.DiagnoserRunMode);
                return new ValueTask<ExecuteResult>(executeResult);
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
            IDiagnoser? diagnoser,
            CompositeInProcessDiagnoser compositeInProcessDiagnoser,
            string executableName,
            IResolver resolver,
            int launchIndex,
            Diagnosers.RunMode diagnoserRunMode)
        {
            using AnonymousPipeServerStream inputFromBenchmark = new(PipeDirection.In, HandleInheritability.Inheritable);
            using AnonymousPipeServerStream acknowledgments = new(PipeDirection.Out, HandleInheritability.Inheritable);

            var startInfo = DotNetCliCommandExecutor.BuildStartInfo(
                customDotNetCliPath,
                artifactsPaths.BinariesDirectoryPath,
                $"{executableName.EscapeCommandLine()} {benchmarkId.ToArguments(inputFromBenchmark.GetClientHandleAsString(), acknowledgments.GetClientHandleAsString(), diagnoserRunMode)}",
                redirectStandardOutput: true,
                redirectStandardInput: false,
                redirectStandardError: false); // #1629

            startInfo.SetEnvironmentVariables(benchmarkCase, resolver);

            using Process process = new() { StartInfo = startInfo };
            using ConsoleExitHandler consoleExitHandler = new(process, logger);
            using AsyncProcessOutputReader processOutputReader = new(process, logOutput: true, logger, readStandardError: false);
            
            Broker broker = new(logger, process, diagnoser, compositeInProcessDiagnoser, benchmarkCase, benchmarkId, inputFromBenchmark, acknowledgments);

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

            broker.ProcessData();

            if (!process.WaitForExit(milliseconds: (int) ExecuteParameters.ProcessExitTimeout.TotalMilliseconds))
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
