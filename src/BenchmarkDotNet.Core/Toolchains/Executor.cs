using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    [PublicAPI("Used by some of our Superusers that implement their own Toolchains (e.g. Kestrel team)")]
    public class Executor : IExecutor
    {
        public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IResolver resolver, IDiagnoser compositeDiagnoser = null)
        {
            var exePath = buildResult.ArtifactsPaths.ExecutablePath;
            var args = compositeDiagnoser == null ? string.Empty : Engine.Signals.DiagnoserIsAttachedParam;

            if (!File.Exists(exePath))
            {
                return new ExecuteResult(false, -1, new string[0], new string[0]);
            }

            return Execute(benchmark, logger, exePath, null, args, compositeDiagnoser, resolver);
        }

        private ExecuteResult Execute(Benchmark benchmark, ILogger logger, string exePath, string workingDirectory, string args, IDiagnoser diagnoser,
            IResolver resolver)
        {
            ConsoleHandler.EnsureInitialized(logger);

            try
            {
                using (var process = new Process { StartInfo = CreateStartInfo(benchmark, exePath, args, workingDirectory, resolver) })
                {
                    var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark);
                    
                    return Execute(process, benchmark, loggerWithDiagnoser, logger);
                }
            }
            finally
            {
                ConsoleHandler.Instance.ClearProcess();
            }
        }

        private ExecuteResult Execute(Process process, Benchmark benchmark, SynchronousProcessOutputLoggerWithDiagnoser loggerWithDiagnoser, ILogger logger)
        {
            logger.WriteLineInfo("// Execute: " + process.StartInfo.FileName + " " + process.StartInfo.Arguments);

            ConsoleHandler.Instance.SetProcess(process);

            process.Start();

            process.EnsureHighPriority(logger);
            if (benchmark.Job.Env.HasValue(EnvMode.AffinityCharacteristic))
            {
                process.EnsureProcessorAffinity(benchmark.Job.Env.Affinity);
            }

            loggerWithDiagnoser.ProcessInput();

            process.WaitForExit(); // should we add timeout here?

            if (process.ExitCode == 0)
            {
                return new ExecuteResult(true, process.ExitCode, loggerWithDiagnoser.LinesWithResults, loggerWithDiagnoser.LinesWithExtraOutput);
            }

            if (loggerWithDiagnoser.LinesWithResults.Any(line => line.Contains("BadImageFormatException")))
                logger.WriteLineError("You are probably missing <PlatformTarget>AnyCPU</PlatformTarget> in your .csproj file.");

            return new ExecuteResult(true, process.ExitCode, new string[0], new string[0]);
        }

        private ProcessStartInfo CreateStartInfo(Benchmark benchmark, string exePath, string args, string workingDirectory, IResolver resolver)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            var runtime = benchmark.Job.Env.HasValue(EnvMode.RuntimeCharacteristic)
                ? benchmark.Job.Env.Runtime
                : RuntimeInformation.GetCurrentRuntime();
                // TODO: use resolver
            switch (runtime)
            {
                case ClrRuntime clr:
                case CoreRuntime core:
                    start.FileName = exePath;
                    start.Arguments = args;
                    break;
                case MonoRuntime mono:
                    start.FileName = mono.CustomPath ?? "mono";
                    start.Arguments = GetMonoArguments(benchmark.Job, exePath, args, resolver);
                    break;
                default:
                    throw new NotSupportedException("Runtime = " + runtime);
            }
            return start;
        }

        private string GetMonoArguments(Job job, string exePath, string args, IResolver resolver)
        {
            // from mono --help: "Usage is: mono [options] program [program-options]"
            return new StringBuilder(30)
                .Append(job.ResolveValue(EnvMode.JitCharacteristic, resolver) == Jit.Llvm ? "--llvm" : "--nollvm")
                .Append(" \"")
                .Append(exePath)
                .Append("\" ")
                .Append(args)
                .ToString();
        }
    }
}