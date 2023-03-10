using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
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
            string exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;

            if (!File.Exists(exePath))
            {
                return ExecuteResult.CreateFailed();
            }

            return Execute(executeParameters.BenchmarkCase, executeParameters.BenchmarkId, executeParameters.Logger, executeParameters.BuildResult.ArtifactsPaths,
                executeParameters.Diagnoser, executeParameters.Resolver, executeParameters.LaunchIndex);
        }

        private static ExecuteResult Execute(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, ArtifactsPaths artifactsPaths,
            IDiagnoser diagnoser, IResolver resolver, int launchIndex)
        {
            try
            {
                using AnonymousPipeServerStream inputFromBenchmark = new (PipeDirection.In, HandleInheritability.Inheritable);
                using AnonymousPipeServerStream acknowledgments = new (PipeDirection.Out, HandleInheritability.Inheritable);

                string args = benchmarkId.ToArguments(inputFromBenchmark.GetClientHandleAsString(), acknowledgments.GetClientHandleAsString());

                using (Process process = new () { StartInfo = CreateStartInfo(benchmarkCase, artifactsPaths, args, resolver) })
                using (ConsoleExitHandler consoleExitHandler = new (process, logger))
                using (AsyncProcessOutputReader processOutputReader = new (process, logOutput: true, logger, readStandardError: false))
                {
                    Broker broker = new (logger, process, diagnoser, benchmarkCase, benchmarkId, inputFromBenchmark, acknowledgments);

                    diagnoser?.Handle(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId));

                    return Execute(process, benchmarkCase, broker, logger, consoleExitHandler, launchIndex, processOutputReader);
                }
            }
            finally
            {
                diagnoser?.Handle(HostSignal.AfterProcessExit, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId));
            }
        }

        private static ExecuteResult Execute(Process process, BenchmarkCase benchmarkCase, Broker broker,
            ILogger logger, ConsoleExitHandler consoleExitHandler, int launchIndex, AsyncProcessOutputReader processOutputReader)
        {
            logger.WriteLineInfo($"// Execute: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                logger.WriteLineError($"// Failed to start the benchmark process: {ex}");

                return new ExecuteResult(true, null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), launchIndex);
            }

            processOutputReader.BeginRead();

            process.EnsureHighPriority(logger);
            if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
            {
                process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
            }

            broker.ProcessData();

            if (!process.WaitForExit(milliseconds: (int)ExecuteParameters.ProcessExitTimeout.TotalMilliseconds))
            {
                logger.WriteLineInfo("// The benchmarking process did not quit on time, it's going to get force killed now.");

                processOutputReader.CancelRead();
                consoleExitHandler.KillProcessTree();
            }
            else
            {
                processOutputReader.StopRead();
            }

            if (broker.Results.Any(line => line.Contains("BadImageFormatException")))
                logger.WriteLineError("You are probably missing <PlatformTarget>AnyCPU</PlatformTarget> in your .csproj file.");

            return new ExecuteResult(true,
                process.HasExited ? process.ExitCode : null,
                process.Id,
                broker.Results,
                broker.PrefixedOutput,
                processOutputReader.GetOutputLines(),
                launchIndex);
        }

        private static ProcessStartInfo CreateStartInfo(BenchmarkCase benchmarkCase, ArtifactsPaths artifactsPaths, string args, IResolver resolver)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                RedirectStandardError = false, // #1629
                CreateNoWindow = true,
                WorkingDirectory = null // by default it's null
            };

            start.SetEnvironmentVariables(benchmarkCase, resolver);

            string exePath = artifactsPaths.ExecutablePath;

            var runtime = benchmarkCase.GetRuntime();

            switch (runtime)
            {
                case ClrRuntime _:
                case CoreRuntime _:
                case NativeAotRuntime _:
                    start.FileName = exePath;
                    start.Arguments = args;
                    break;
                case MonoRuntime mono:
                    start.FileName = mono.CustomPath ?? "mono";
                    start.Arguments = GetMonoArguments(benchmarkCase.Job, exePath, args, resolver);
                    break;
                case MonoAotLLVMRuntime _:
                    start.FileName = exePath;
                    start.Arguments = args;
                    start.WorkingDirectory = artifactsPaths.BinariesDirectoryPath;
                    break;
                case CustomRuntime _:
                    start.FileName = exePath;
                    start.Arguments = args;
                    break;
                default:
                    throw new NotSupportedException("Runtime = " + runtime);
            }
            return start;
        }

        private static string GetMonoArguments(Job job, string exePath, string args, IResolver resolver)
        {
            var arguments = job.HasValue(InfrastructureMode.ArgumentsCharacteristic)
                ? job.ResolveValue(InfrastructureMode.ArgumentsCharacteristic, resolver).OfType<MonoArgument>().ToArray()
                : Array.Empty<MonoArgument>();

            // from mono --help: "Usage is: mono [options] program [program-options]"
            var builder = new StringBuilder(30);

            builder.Append(job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver) == Jit.Llvm ? "--llvm" : "--nollvm");

            foreach (var argument in arguments)
                builder.Append($" {argument.TextRepresentation}");

            builder.Append($" \"{exePath}\" ");
            builder.Append(args);

            return builder.ToString();
        }
    }
}
