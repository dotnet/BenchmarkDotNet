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
using System.Diagnostics;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public class DotNetCliExecutor(string customDotNetCliPath) : IExecutor
    {
        public async ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters, CancellationToken cancellationToken)
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
                    executeParameters.DiagnoserRunMode,
                    cancellationToken
                ).ConfigureAwait(true);
            }
            finally
            {
                await executeParameters.Diagnoser.HandleAsync(
                    HostSignal.AfterProcessExit,
                    new DiagnoserActionParameters(null, executeParameters.BenchmarkCase, executeParameters.BenchmarkId),
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }

        private async ValueTask<ExecuteResult> Execute(BenchmarkCase benchmarkCase,
            BenchmarkId benchmarkId,
            ILogger logger,
            ArtifactsPaths artifactsPaths,
            IDiagnoser diagnoser,
            CompositeInProcessDiagnoser compositeInProcessDiagnoser,
            string executableName,
            IResolver resolver,
            int launchIndex,
            Diagnosers.RunMode diagnoserRunMode,
            CancellationToken cancellationToken)
        {
            using var tcplistener = new TcpListener();
            var port = tcplistener.StartAndGetPort();

            var startInfo = DotNetCliCommandExecutor.BuildStartInfo(
                customDotNetCliPath,
                artifactsPaths.BinariesDirectoryPath,
                $"{executableName.EscapeCommandLine()} {benchmarkId.ToArguments(port, diagnoserRunMode)}",
                redirectStandardOutput: true,
                redirectStandardInput: false,
                redirectStandardError: true);

            startInfo.SetEnvironmentVariables(benchmarkCase, resolver);

            using Process process = new() { StartInfo = startInfo };
            using AsyncProcessOutputReader processOutputReader = new(process, stdOutLogger: logger, stdErrLogger: logger, cacheStandardError: false);

            List<string> results;
            List<string> prefixedOutput;
            await using (new ProcessCleanupHelper(process, processOutputReader, logger).ConfigureAwait(false))
            {
                using Broker broker = new(logger, process, diagnoser, compositeInProcessDiagnoser, benchmarkCase, benchmarkId, tcplistener);

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

                await broker.ProcessData(cancellationToken).ConfigureAwait(false);

                results = broker.Results;
                prefixedOutput = broker.PrefixedOutput;

                if (!process.WaitForExit(milliseconds: (int)ExecuteParameters.ProcessExitTimeout.TotalMilliseconds))
                {
                    logger.WriteLineInfo($"// The benchmarking process did not quit within {ExecuteParameters.ProcessExitTimeout.TotalSeconds} seconds, it's going to get force killed now.");
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
