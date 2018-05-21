using System;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;
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
                
                return new ExecuteResult(false, -1, Array.Empty<string>(), Array.Empty<string>());
            }

            ConsoleHandler.EnsureInitialized(executeParameters.Logger);

            try
            {
                return Execute(
                    executeParameters.Benchmark,
                    executeParameters.BenchmarkId,
                    executeParameters.Logger, 
                    executeParameters.BuildResult.ArtifactsPaths, 
                    executeParameters.Diagnoser, 
                    Path.GetFileName(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath), 
                    executeParameters.Config,
                    executeParameters.Resolver);
            }
            finally
            {
                ConsoleHandler.Instance.ClearProcess();
            }
        }

        private ExecuteResult Execute(Benchmark benchmark, BenchmarkId benchmarkId, ILogger logger, ArtifactsPaths artifactsPaths, IDiagnoser diagnoser, string executableName, IConfig config, IResolver resolver)
        {
            var startInfo = DotNetCliCommandExecutor.BuildStartInfo(
                CustomDotNetCliPath,
                artifactsPaths.BinariesDirectoryPath,
                $"{executableName} {benchmarkId.ToArgument()}",
                redirectStandardInput: true);

            startInfo.SetEnvironmentVariables(benchmark, resolver);

            using (var process = new Process { StartInfo = startInfo })
            {
                var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark, benchmarkId, config);

                ConsoleHandler.Instance.SetProcess(process);

                process.Start();

                process.EnsureHighPriority(logger);
                if (benchmark.Job.Env.HasValue(EnvMode.AffinityCharacteristic))
                {
                    process.TrySetAffinity(benchmark.Job.Env.Affinity, logger);
                }

                loggerWithDiagnoser.ProcessInput();
                string standardError = process.StandardError.ReadToEnd();

                process.WaitForExit(); // should we add timeout here?

                if (process.ExitCode == 0)
                {
                    return new ExecuteResult(true, process.ExitCode, loggerWithDiagnoser.LinesWithResults, loggerWithDiagnoser.LinesWithExtraOutput);
                }

                if (!string.IsNullOrEmpty(standardError))
                {
                    logger.WriteError(standardError);
                }

                return new ExecuteResult(true, process.ExitCode, Array.Empty<string>(), Array.Empty<string>());
            }
        }
    }
}