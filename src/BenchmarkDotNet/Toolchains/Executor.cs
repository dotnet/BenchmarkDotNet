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
using System.ComponentModel;
using System.Diagnostics;

namespace BenchmarkDotNet.Toolchains
{
    [PublicAPI("Used by some of our Superusers that implement their own Toolchains (e.g. Kestrel team)")]
    public class Executor : IExecutor
    {
        public static readonly Executor Instance = new();

        public async ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters, CancellationToken cancellationToken)
        {
            string exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;

            return !File.Exists(exePath)
                ? ExecuteResult.CreateFailed()
                : await Execute(executeParameters.BenchmarkCase, executeParameters.BenchmarkId, executeParameters.Logger, executeParameters.BuildResult.ArtifactsPaths,
                    executeParameters.Diagnoser, executeParameters.CompositeInProcessDiagnoser, executeParameters.Resolver, executeParameters.LaunchIndex,
                    executeParameters.DiagnoserRunMode, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<ExecuteResult> Execute(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, ArtifactsPaths artifactsPaths,
            IDiagnoser diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser, IResolver resolver, int launchIndex,
            Diagnosers.RunMode diagnoserRunMode, CancellationToken cancellationToken)
        {
            try
            {
                return await ExecuteCore(benchmarkCase, benchmarkId, logger, artifactsPaths, diagnoser, compositeInProcessDiagnoser, resolver, launchIndex, diagnoserRunMode, cancellationToken)
                    .ConfigureAwait();
            }
            finally
            {
                await diagnoser.HandleAsync(HostSignal.AfterProcessExit, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async ValueTask<ExecuteResult> ExecuteCore(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, ArtifactsPaths artifactsPaths,
            IDiagnoser diagnoser, CompositeInProcessDiagnoser compositeInProcessDiagnoser, IResolver resolver, int launchIndex,
            Diagnosers.RunMode diagnoserRunMode, CancellationToken cancellationToken)
        {
            using var tcplistener = new TcpListener();
            var port = tcplistener.StartAndGetPort();

            string args = benchmarkId.ToArguments(port, diagnoserRunMode);

            using Process process = new() { StartInfo = CreateStartInfo(benchmarkCase, artifactsPaths, args, resolver) };
            using AsyncProcessOutputReader processOutputReader = new(process, stdOutLogger: logger, stdErrLogger: logger, cacheStandardError: false);

            List<string> results;
            List<string> prefixedOutput;
            await using (new ProcessCleanupHelper(process, processOutputReader, logger).ConfigureAwait(false))
            {
                using Broker broker = new(logger, process, diagnoser, compositeInProcessDiagnoser, benchmarkCase, benchmarkId, tcplistener);

                await diagnoser.HandleAsync(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId), cancellationToken).ConfigureAwait();

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

                await broker.Diagnoser.HandleAsync(HostSignal.AfterProcessStart, broker.DiagnoserActionParameters, cancellationToken).ConfigureAwait();

                processOutputReader.BeginRead();

                process.EnsureHighPriority(logger);
                if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
                {
                    process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
                }

                await broker.ProcessData(cancellationToken).ConfigureAwait(false);

                results = broker.Results;
                prefixedOutput = broker.PrefixedOutput;

                if (!process.WaitForExit(milliseconds: (int) ExecuteParameters.ProcessExitTimeout.TotalMilliseconds))
                {
                    logger.WriteLineInfo("// The benchmarking process did not quit on time, it's going to get force killed now.");
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

        [PublicAPI]
        protected virtual ProcessStartInfo CreateStartInfo(BenchmarkCase benchmarkCase, ArtifactsPaths artifactsPaths, string args, IResolver resolver)
        {
            var start = new ProcessStartInfo
            {
                FileName = artifactsPaths.ExecutablePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = null // by default it's null
            };
            start.SetEnvironmentVariables(benchmarkCase, resolver);
            return start;
        }
    }
}
