using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
        private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(2);

        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            if (!File.Exists(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath))
            {
                executeParameters.Logger.WriteLineError($"Did not find {executeParameters.BuildResult.ArtifactsPaths.ExecutablePath}, but the folder contained:");
                foreach (var file in new DirectoryInfo(executeParameters.BuildResult.ArtifactsPaths.BinariesDirectoryPath).GetFiles("*.*"))
                    executeParameters.Logger.WriteLineError(file.Name);

                return new ExecuteResult(false, -1, default, Array.Empty<string>(), Array.Empty<string>());
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
                    executeParameters.Resolver);
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
                                      IResolver resolver)
        {
            var startInfo = DotNetCliCommandExecutor.BuildStartInfo(
                CustomDotNetCliPath,
                artifactsPaths.BinariesDirectoryPath,
                $"{executableName.Escape()} {benchmarkId.ToArguments()}",
                redirectStandardInput: true,
                redirectStandardError: false); // #1629

            startInfo.SetEnvironmentVariables(benchmarkCase, resolver);

            using (var process = new Process { StartInfo = startInfo })
            using (var consoleExitHandler = new ConsoleExitHandler(process, logger))
            {
                var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmarkCase, benchmarkId);

                logger.WriteLineInfo($"// Execute: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

                diagnoser?.Handle(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId));

                process.Start();

                process.EnsureHighPriority(logger);
                if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
                {
                    process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
                }

                loggerWithDiagnoser.ProcessInput();

                if (!process.WaitForExit(milliseconds: (int)ProcessExitTimeout.TotalMilliseconds))
                {
                    logger.WriteLineInfo($"// The benchmarking process did not quit within {ProcessExitTimeout.TotalSeconds} seconds, it's going to get force killed now.");

                    consoleExitHandler.KillProcessTree();
                }

                return new ExecuteResult(true, process.HasExited ? (int?)process.ExitCode : null, process.Id, loggerWithDiagnoser.LinesWithResults, loggerWithDiagnoser.LinesWithExtraOutput);
            }
        }
    }
}
