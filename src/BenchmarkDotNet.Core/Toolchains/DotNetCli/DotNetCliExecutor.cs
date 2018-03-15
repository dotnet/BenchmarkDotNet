using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
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
            var executableName = $"{executeParameters.BuildResult.ArtifactsPaths.ProgramName}.dll";
            if (!File.Exists(Path.Combine(executeParameters.BuildResult.ArtifactsPaths.BinariesDirectoryPath, executableName)))
            {
                executeParameters.Logger.WriteLineError($"Did not find {executableName} in {executeParameters.BuildResult.ArtifactsPaths.BinariesDirectoryPath}, but the folder contained:");
                foreach (var file in new DirectoryInfo(executeParameters.BuildResult.ArtifactsPaths.BinariesDirectoryPath).GetFiles("*.*"))
                    executeParameters.Logger.WriteLineError(file.Name);
                
                return new ExecuteResult(false, -1, Array.Empty<string>(), Array.Empty<string>());
            }

            ConsoleHandler.EnsureInitialized(executeParameters.Logger);

            try
            {
                return Execute(
                    executeParameters.Benchmark, 
                    executeParameters.Logger, 
                    executeParameters.BuildResult.ArtifactsPaths, 
                    executeParameters.Diagnoser, 
                    executableName, 
                    executeParameters.Config,
                    executeParameters.Resolver);
            }
            finally
            {
                ConsoleHandler.Instance.ClearProcess();
            }
        }

        private ExecuteResult Execute(Benchmark benchmark, ILogger logger, ArtifactsPaths artifactsPaths, IDiagnoser diagnoser, string executableName, IConfig config, IResolver resolver)
        {
            var startInfo = DotNetCliCommandExecutor.BuildStartInfo(
                CustomDotNetCliPath,
                artifactsPaths.BinariesDirectoryPath,
                BuildArgs(diagnoser, executableName),
                redirectStandardInput: true);

            startInfo.SetEnvironmentVariables(benchmark, resolver);

            using (var process = new Process { StartInfo = startInfo })
            {
                var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark, config);

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

        private static string BuildArgs(IDiagnoser diagnoser, string executableName)
        {
            var args = new StringBuilder(50);

            args.AppendFormat(executableName);

            if (diagnoser != null)
            {
                args.Append($" {Engine.Signals.DiagnoserIsAttachedParam}");
            }

            return args.ToString();
        }
    }
}