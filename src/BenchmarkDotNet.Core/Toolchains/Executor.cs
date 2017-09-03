using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    [PublicAPI("Used by some of our Superusers that implement their own Toolchains (e.g. Kestrel team)")]
    public class Executor : IExecutor
    {
        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            var exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;
            var args = executeParameters.Diagnoser == null ? string.Empty : Engine.Signals.DiagnoserIsAttachedParam;

            if (!File.Exists(exePath))
            {
                return new ExecuteResult(false, -1, Array.Empty<string>(), Array.Empty<string>());
            }

            return Execute(executeParameters.Benchmark, executeParameters.Logger, exePath, null, args, executeParameters.Diagnoser, executeParameters.Resolver, executeParameters.Config);
        }

        private ExecuteResult Execute(Benchmark benchmark, ILogger logger, string exePath, string workingDirectory, string args, IDiagnoser diagnoser, IResolver resolver, IConfig config)
        {
            ConsoleHandler.EnsureInitialized(logger);

            try
            {
                using (var process = new Process { StartInfo = CreateStartInfo(benchmark, exePath, args, workingDirectory, resolver) })
                {
                    var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmark, config);

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
                process.TrySetAffinity(benchmark.Job.Env.Affinity, logger);
            }

            loggerWithDiagnoser.ProcessInput();

            process.WaitForExit(); // should we add timeout here?

            if (process.ExitCode == 0)
            {
                return new ExecuteResult(true, process.ExitCode, loggerWithDiagnoser.LinesWithResults, loggerWithDiagnoser.LinesWithExtraOutput);
            }

            if (loggerWithDiagnoser.LinesWithResults.Any(line => line.Contains("BadImageFormatException")))
                logger.WriteLineError("You are probably missing <PlatformTarget>AnyCPU</PlatformTarget> in your .csproj file.");

            return new ExecuteResult(true, process.ExitCode, Array.Empty<string>(), Array.Empty<string>());
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

            start.SetEnvironmentVariables(benchmark, resolver);

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
            var arguments = job.HasValue(InfrastructureMode.ArgumentsCharacteristic)
                ? job.ResolveValue(InfrastructureMode.ArgumentsCharacteristic, resolver).OfType<MonoArgument>().ToArray()
                : Array.Empty<MonoArgument>();

            // from mono --help: "Usage is: mono [options] program [program-options]"
            var builder = new StringBuilder(30);

            builder.Append(job.ResolveValue(EnvMode.JitCharacteristic, resolver) == Jit.Llvm ? "--llvm" : "--nollvm");

            foreach (var argument in arguments)
                builder.Append($" {argument.TextRepresentation}");

            builder.Append($" \"{exePath}\" ");
            builder.Append(args);

            return builder.ToString();
        }
    }
}