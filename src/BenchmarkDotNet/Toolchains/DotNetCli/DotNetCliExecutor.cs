using System;
using System.Diagnostics;
using System.IO;
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

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public class DotNetCliExecutor : IExecutor
    {
        public DotNetCliExecutor(string customDotNetCliPath) => CustomDotNetCliPath = customDotNetCliPath;

        private string CustomDotNetCliPath { get; }

        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            try
            {
                return Execute(
                    executeParameters.BuildResult.ExecutablePath,
                    executeParameters.BenchmarkCase,
                    executeParameters.BenchmarkId,
                    executeParameters.Logger,
                    executeParameters.Diagnoser,
                    executeParameters.Resolver);
            }
            finally
            {
                executeParameters.Diagnoser?.Handle(
                    HostSignal.AfterProcessExit,
                    new DiagnoserActionParameters(null, executeParameters.BenchmarkCase, executeParameters.BenchmarkId));
            }
        }

        private ExecuteResult Execute(string executablePath, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, IDiagnoser diagnoser, IResolver resolver)
        {
            var startInfo = DotNetCliCommandExecutor.BuildStartInfo(
                CustomDotNetCliPath,
                Path.GetDirectoryName(executablePath),
                $"{Path.GetFileName(executablePath).Escape()} {benchmarkId.ToArguments()}",
                redirectStandardInput: true);

            startInfo.SetEnvironmentVariables(benchmarkCase, resolver);

            using (var process = new Process { StartInfo = startInfo })
            using (new ConsoleExitHandler(process, logger))
            {
                var loggerWithDiagnoser = new SynchronousProcessOutputLoggerWithDiagnoser(logger, process, diagnoser, benchmarkCase, benchmarkId);

                logger.WriteLineInfo($"// Execute: {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

                diagnoser?.Handle(HostSignal.BeforeProcessStart, new DiagnoserActionParameters(process, benchmarkCase, benchmarkId));

                process.Start();

                process.EnsureHighPriority(logger);
                if (benchmarkCase.Job.Environment.HasValue(EnvironmentMode.AffinityCharacteristic))
                {
                    process.TrySetAffinity(benchmarkCase.Job.Environment.Affinity, logger);
                }

                loggerWithDiagnoser.ProcessInput();
                string standardError = process.StandardError.ReadToEnd();

                process.WaitForExit(); // should we add timeout here?

                if (process.ExitCode == 0)
                {
                    return new ExecuteResult(true, process.ExitCode, process.Id, loggerWithDiagnoser.LinesWithResults, loggerWithDiagnoser.LinesWithExtraOutput);
                }

                if (!string.IsNullOrEmpty(standardError))
                {
                    logger.WriteError(standardError);
                }

                return new ExecuteResult(true, process.ExitCode, process.Id, Array.Empty<string>(), Array.Empty<string>());
            }
        }
    }
}