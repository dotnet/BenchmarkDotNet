using System.Diagnostics;
using System.IO;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public class DotNetCliExecutor : IExecutor
    {
        public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IResolver resolver, IDiagnoser diagnoser = null)
        {
            var executableName = $"{buildResult.ArtifactsPaths.ProgramName}.dll";
            if (!File.Exists(Path.Combine(buildResult.ArtifactsPaths.BinariesDirectoryPath, executableName)))
            {
                logger.WriteError($"Did not find {executableName} in {buildResult.ArtifactsPaths.BinariesDirectoryPath}, but the folder contained:");
                foreach (var file in new DirectoryInfo(buildResult.ArtifactsPaths.BinariesDirectoryPath).GetFiles("*.*"))
                    logger.WriteLineError(file.Name);
                
                return new ExecuteResult(false, -1, new string[0], new string[0]);
            }

            ConsoleHandler.EnsureInitialized(logger);

            try
            {
                return Execute(benchmark, logger, buildResult.ArtifactsPaths, diagnoser, executableName);
            }
            finally
            {
                ConsoleHandler.Instance.ClearProcess();
            }
        }

        private ExecuteResult Execute(Benchmark benchmark, ILogger logger, ArtifactsPaths artifactsPaths, IDiagnoser diagnoser, string executableName)
        {
            using (var process = new Process
            {
                StartInfo = DotNetCliCommandExecutor.BuildStartInfo(
                    artifactsPaths.BinariesDirectoryPath, 
                    BuildArgs(diagnoser, executableName))
            })
            {
                var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark);

                ConsoleHandler.Instance.SetProcess(process);

                process.Start();

                process.EnsureHighPriority(logger);
                if (benchmark.Job.Env.HasValue(EnvMode.AffinityCharacteristic))
                {
                    process.EnsureProcessorAffinity(benchmark.Job.Env.Affinity);
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

                return new ExecuteResult(true, process.ExitCode, new string[0], new string[0]);
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