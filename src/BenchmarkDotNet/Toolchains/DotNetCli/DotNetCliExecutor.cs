using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public async ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters)
        {
            if (!File.Exists(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath))
            {
                executeParameters.Logger.WriteLineError($"Did not find {executeParameters.BuildResult.ArtifactsPaths.ExecutablePath}, but the folder contained:");
                foreach (var file in new DirectoryInfo(executeParameters.BuildResult.ArtifactsPaths.BinariesDirectoryPath).GetFiles("*.*"))
                {
                    executeParameters.Logger.WriteLineError(file.Name);
                }

                return ExecuteResult.CreateFailed();
            }

            try
            {
                return await Execute(
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
            }
            finally
            {
                executeParameters.Diagnoser?.Handle(
                    HostSignal.AfterProcessExit,
                    new DiagnoserActionParameters(null, executeParameters.BenchmarkCase, executeParameters.BenchmarkId));
            }
        }

        private async ValueTask<ExecuteResult> Execute(BenchmarkCase benchmarkCase,
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
            using var pipe = NamedPipeHost.GetPipeServerStream(benchmarkId, out string pipeName);

            var startInfo = DotNetCliCommandExecutor.BuildStartInfo(
                customDotNetCliPath,
                artifactsPaths.BinariesDirectoryPath,
                $"{executableName.EscapeCommandLine()} {benchmarkId.ToArguments(pipeName, diagnoserRunMode)}",
                redirectStandardOutput: true,
                redirectStandardInput: false,
                redirectStandardError: false); // #1629

            startInfo.SetEnvironmentVariables(benchmarkCase, resolver);

            using Process process = new() { StartInfo = startInfo };
            using ConsoleExitHandler consoleExitHandler = new(process, logger);
            using AsyncProcessOutputReader processOutputReader = new(process, logOutput: true, logger, readStandardError: false);

            List<string> results;
            List<string> prefixedOutput;
            using (Broker broker = new(logger, process, diagnoser, compositeInProcessDiagnoser, benchmarkCase, benchmarkId, pipe))
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
                results,
                prefixedOutput,
                processOutputReader.GetOutputLines(),
                launchIndex);
        }
    }
}
