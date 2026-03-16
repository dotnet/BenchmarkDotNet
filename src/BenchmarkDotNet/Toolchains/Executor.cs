using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public async ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters, CancellationToken cancellationToken)
        {
            string exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;

            return !File.Exists(exePath)
                ? ExecuteResult.CreateFailed()
                : await Execute(executeParameters.BenchmarkCase, executeParameters.BenchmarkId, executeParameters.Logger, executeParameters.BuildResult.ArtifactsPaths,
                    executeParameters.Diagnoser, executeParameters.CompositeInProcessDiagnoser, executeParameters.Resolver, executeParameters.LaunchIndex,
                    executeParameters.DiagnoserRunMode, cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask<ExecuteResult> Execute(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, ArtifactsPaths artifactsPaths,
            IDiagnoser diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser, IResolver resolver, int launchIndex,
            Diagnosers.RunMode diagnoserRunMode, CancellationToken cancellationToken)
        {
            try
            {
                return await ExecuteCore(benchmarkCase, benchmarkId, logger, artifactsPaths, diagnoser, compositeInProcessDiagnoser, resolver, launchIndex, diagnoserRunMode, cancellationToken)
                    .ConfigureAwait(true);
            }
            finally
            {
                await diagnoser.HandleAsync(HostSignal.AfterProcessExit, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private static async ValueTask<ExecuteResult> ExecuteCore(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, ArtifactsPaths artifactsPaths,
            IDiagnoser diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser, IResolver resolver, int launchIndex,
            Diagnosers.RunMode diagnoserRunMode, CancellationToken cancellationToken)
        {
            using var tcplistener = new TcpListener();
            var port = tcplistener.StartAndGetPort();

            string args = benchmarkId.ToArguments(port, diagnoserRunMode);

            using Process process = new() { StartInfo = CreateStartInfo(benchmarkCase, artifactsPaths, args, resolver) };
            using ProcessCleanupHelper processCleanupHelper = new(process, logger);
            using AsyncProcessOutputReader processOutputReader = new(process, logOutput: true, logger, readStandardError: true);

            List<string> results;
            List<string> prefixedOutput;
            bool processOutputStarted = false;
            try
            {
                using Broker broker = new(logger, process, diagnoser, compositeInProcessDiagnoser, benchmarkCase, benchmarkId, tcplistener);

                await diagnoser.HandleAsync(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId), cancellationToken).ConfigureAwait(true);

                logger.WriteLineInfo($"// Execute: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

                try
                {
                    process.Start();
                }
                catch (Win32Exception ex)
                {
                    logger.WriteLineError($"// Failed to start the benchmark process: {ex}");

                    return new ExecuteResult(true, null, null, [], [], [], launchIndex);
                }

                await broker.Diagnoser.HandleAsync(HostSignal.AfterProcessStart, broker.DiagnoserActionParameters, cancellationToken).ConfigureAwait(true);

                processOutputReader.BeginRead();
                processOutputStarted = true;

                process.EnsureHighPriority(logger);
                if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
                {
                    process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
                }

                await broker.ProcessData(cancellationToken).ConfigureAwait(false);

                results = broker.Results;
                prefixedOutput = broker.PrefixedOutput;
            }
            finally
            {
                if (processOutputStarted)
                {
                    if (!process.WaitForExit(milliseconds: (int) ExecuteParameters.ProcessExitTimeout.TotalMilliseconds))
                    {
                        logger.WriteLineInfo("// The benchmarking process did not quit on time, it's going to get force killed now.");

                        processOutputReader.CancelRead();
                        processCleanupHelper.KillProcessTree();
                    }
                    else
                    {
                        await processOutputReader.StopReadAsync().ConfigureAwait(false);
                    }
                }
            }

            if (process.HasExited && process.ExitCode != 0)
            {
                string stderr = processOutputReader.GetErrorText();
                if (!string.IsNullOrEmpty(stderr))
                {
                    logger.WriteLineError($"// Benchmark process stderr: {stderr}");
                }
            }

            if (results.Any(line => line.Contains("BadImageFormatException")))
                logger.WriteLineError("You are probably missing <PlatformTarget>AnyCPU</PlatformTarget> in your .csproj file.");

            return new ExecuteResult(true,
                process.HasExited ? process.ExitCode : null,
                process.Id,
                results,
                prefixedOutput,
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
                RedirectStandardError = true,
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
                case R2RRuntime _:
                    start.FileName = exePath;
                    start.Arguments = args;
                    break;
                case MonoRuntime mono:
                    start.FileName = mono.CustomPath.IsNotBlank() ? mono.CustomPath : "mono";
                    start.Arguments = GetMonoArguments(benchmarkCase.Job, exePath, args, resolver);
                    break;
                case MonoAotLLVMRuntime _:
                    start.FileName = exePath;
                    start.Arguments = args;
                    start.WorkingDirectory = Path.Combine(artifactsPaths.BinariesDirectoryPath, "publish");
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
                ? job.ResolveValue(InfrastructureMode.ArgumentsCharacteristic, resolver)!.OfType<MonoArgument>().ToArray()
                : [];

            // from mono --help: "Usage is: mono [options] program [program-options]"
            var builder = new StringBuilder(30);

            builder.Append(job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver) == Jit.Llvm ? "--llvm" : "--nollvm");

            foreach (var argument in arguments)
            {
                builder.Append($" {argument.TextRepresentation}");
            }

            builder.Append($" \"{exePath}\" ");
            builder.Append(args);

            return builder.ToString();
        }
    }
}
