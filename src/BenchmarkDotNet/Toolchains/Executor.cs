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
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
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
            string exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;
            string args = executeParameters.BenchmarkId.ToArguments();

            if (!File.Exists(exePath))
            {
                return new ExecuteResult(false, -1, default, Array.Empty<string>(), Array.Empty<string>());
            }

            return Execute(executeParameters.BenchmarkCase, executeParameters.BenchmarkId, executeParameters.Logger, executeParameters.BuildResult.ArtifactsPaths, args, executeParameters.Diagnoser, executeParameters.Resolver);
        }

        private ExecuteResult Execute(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, ArtifactsPaths artifactsPaths, string args, IDiagnoser diagnoser, IResolver resolver)
        {
            try
            {
                using (var process = new Process { StartInfo = CreateStartInfo(benchmarkCase, artifactsPaths, args, resolver) })
                using (var consoleExitHandler = new ConsoleExitHandler(process, logger))
                {
                    var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmarkCase, benchmarkId);

                    diagnoser?.Handle(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId));

                    return Execute(process, benchmarkCase, loggerWithDiagnoser, logger, consoleExitHandler);
                }
            }
            finally
            {
                diagnoser?.Handle(HostSignal.AfterProcessExit, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId));
            }
        }

        private ExecuteResult Execute(Process process, BenchmarkCase benchmarkCase, SynchronousProcessOutputLoggerWithDiagnoser loggerWithDiagnoser, ILogger logger, ConsoleExitHandler consoleExitHandler)
        {
            logger.WriteLineInfo($"// Execute: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

            process.Start();

            process.EnsureHighPriority(logger);
            if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
            {
                process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
            }

            loggerWithDiagnoser.ProcessInput();

            if (!process.WaitForExit(milliseconds: 250))
            {
                logger.WriteLineInfo("// The benchmarking process did not quit on time, it's going to get force killed now.");

                consoleExitHandler.KillProcessTree();
            }

            if (loggerWithDiagnoser.LinesWithResults.Any(line => line.Contains("BadImageFormatException")))
                logger.WriteLineError("You are probably missing <PlatformTarget>AnyCPU</PlatformTarget> in your .csproj file.");

            return new ExecuteResult(true, process.ExitCode, process.Id, loggerWithDiagnoser.LinesWithResults, loggerWithDiagnoser.LinesWithExtraOutput);
        }

        private ProcessStartInfo CreateStartInfo(BenchmarkCase benchmarkCase, ArtifactsPaths artifactsPaths, string args, IResolver resolver)
        {
            var start = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = false, // #1629
                CreateNoWindow = true,
                WorkingDirectory = null // by default it's null
            };

            start.SetEnvironmentVariables(benchmarkCase, resolver);

            string exePath = artifactsPaths.ExecutablePath;

            var runtime = benchmarkCase.GetRuntime();
            // TODO: use resolver

            switch (runtime)
            {
                case ClrRuntime _:
                case CoreRuntime _:
                case CoreRtRuntime _:
                    start.FileName = exePath;
                    start.Arguments = args;
                    break;
                case MonoRuntime mono:
                    start.FileName = mono.CustomPath ?? "mono";
                    start.Arguments = GetMonoArguments(benchmarkCase.Job, exePath, args, resolver);
                    break;
                case WasmRuntime wasm:
                    start.FileName = wasm.JavaScriptEngine;
                    start.RedirectStandardInput = false;
                    start.Arguments = $"{wasm.JavaScriptEngineArguments} runtime.js -- --run {artifactsPaths.ProgramName}.dll {args} ";
                    start.WorkingDirectory = artifactsPaths.BinariesDirectoryPath;
                    break;
                case MonoAotLLVMRuntime _:
                    start.FileName = exePath;
                    start.Arguments = args;
                    start.WorkingDirectory = artifactsPaths.BinariesDirectoryPath;
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

            builder.Append(job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver) == Jit.Llvm ? "--llvm" : "--nollvm");

            foreach (var argument in arguments)
                builder.Append($" {argument.TextRepresentation}");

            builder.Append($" \"{exePath}\" ");
            builder.Append(args);

            return builder.ToString();
        }
    }
}
