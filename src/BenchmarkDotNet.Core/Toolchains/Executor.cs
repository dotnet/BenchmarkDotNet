using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    internal class Executor : IExecutor
    {
        // This needs to be static, so that we can share a single handler amongst all instances of Executor's
        private static ConsoleHandler consoleHandler;

        public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IResolver resolver, IDiagnoser compositeDiagnoser = null)
        {
            var exePath = buildResult.ArtifactsPaths.ExecutablePath;
            var args = string.Empty;

            if (!File.Exists(exePath))
            {
                return new ExecuteResult(false, -1, new string[0]);
            }

            return Execute(benchmark, logger, exePath, null, args, compositeDiagnoser, resolver);
        }

        private ExecuteResult Execute(Benchmark benchmark, ILogger logger, string exeName, string workingDirectory, string args, IDiagnoser diagnoser,
            IResolver resolver)
        {
            if (consoleHandler == null)
            {
                consoleHandler = new ConsoleHandler(logger);
                Console.CancelKeyPress += consoleHandler.EventHandler;
            }

            try
            {
                using (var process = new Process { StartInfo = CreateStartInfo(benchmark, exeName, args, workingDirectory, resolver) })
                {
                    var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark);

                    return Execute(process, benchmark, loggerWithDiagnoser, diagnoser, logger);
                }
            }
            finally
            {
                consoleHandler.ClearProcess();
            }
        }

        private ExecuteResult Execute(Process process, Benchmark benchmark, SynchronousProcessOutputLoggerWithDiagnoser loggerWithDiagnoser,
            IDiagnoser compositeDiagnoser, ILogger logger)
        {
            consoleHandler.SetProcess(process);

            process.Start();

            compositeDiagnoser?.ProcessStarted(process);

            process.EnsureHighPriority(logger);
            if (!benchmark.Job.Env.Affinity.IsDefault)
            {
                process.EnsureProcessorAffinity(benchmark.Job.Env.Affinity.SpecifiedValue);
            }

            loggerWithDiagnoser.ProcessInput();

            process.WaitForExit(); // should we add timeout here?

            compositeDiagnoser?.ProcessStopped(process);

            if (process.ExitCode == 0)
            {
                return new ExecuteResult(true, process.ExitCode, loggerWithDiagnoser.Lines);
            }

            return new ExecuteResult(true, process.ExitCode, new string[0]);
        }

        private ProcessStartInfo CreateStartInfo(Benchmark benchmark, string exeName, string args, string workingDirectory, IResolver resolver)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            var runtime = benchmark.Job.Env.Runtime.IsDefault ? RuntimeInformation.GetCurrentRuntime() : benchmark.Job.Env.Runtime.SpecifiedValue;
                // TODO: use resolver
            switch (runtime)
            {
                case Runtime.Clr:
                case Runtime.Core:
                    start.FileName = exeName;
                    start.Arguments = args;
                    break;
                case Runtime.Mono:
                    start.FileName = "mono";
                    start.Arguments = GetMonoArguments(benchmark.Job, exeName, args, resolver);
                    break;
                default:
                    throw new NotSupportedException("Runtime = " + runtime);
            }
            return start;
        }

        private string GetMonoArguments(Job job, string exeName, string args, IResolver resolver)
        {
            // from mono --help: "Usage is: mono [options] program [program-options]"
            return new StringBuilder(30)
                .Append(job.Env.Jit.Resolve(resolver) == Jit.Llvm ? "--llvm" : "--nollvm")
                .Append(' ')
                .Append(exeName)
                .Append(' ')
                .Append(args)
                .ToString();
        }
    }
}