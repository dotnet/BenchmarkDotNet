using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    /// <summary>
    /// very simple, experimental version
    /// limitations: can not set process priority and/or affinity + can not break with ctrl+c (verify)
    /// </summary>
    public class DotNetRunExecutor : IExecutor
    {
        public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IResolver resolver, IDiagnoser diagnoser = null)
        {
            return Execute(benchmark, logger, buildResult.ArtifactsPaths.BuildArtifactsDirectoryPath, diagnoser, resolver);
        }

        private ExecuteResult Execute(Benchmark benchmark, ILogger logger, string workingDirectory, IDiagnoser diagnoser,
            IResolver resolver)
        {
            using (var process = new Process { StartInfo = DotNetCliCommandExecutor.BuildStartInfo(workingDirectory, "run --configuration Release") }) // there is only one framework in the .csproj, no need to specify it
            {
                var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark);

                //consoleHandler.SetProcess(process); // todo

                process.Start();

                //process.EnsureHighPriority(logger); // todo
                //if (benchmark.Job.Env.HasValue(EnvMode.AffinityCharacteristic))
                //{
                //    process.EnsureProcessorAffinity(benchmark.Job.Env.Affinity);
                //}

                loggerWithDiagnoser.ProcessInput();

                process.WaitForExit(); // should we add timeout here?

                if (process.ExitCode == 0)
                {
                    return new ExecuteResult(true, process.ExitCode, loggerWithDiagnoser.LinesWithResults, loggerWithDiagnoser.LinesWithExtraOutput);
                }

                return new ExecuteResult(true, process.ExitCode, new string[0], new string[0]);
            }
        }
    }
}