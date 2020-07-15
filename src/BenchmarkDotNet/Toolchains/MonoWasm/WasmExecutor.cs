using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    [PublicAPI]
    public class WasmExecutor : IExecutor
    {
        public WasmExecutor(string customDotNetCliPath, string javaScriptEngine, string javaScriptEngineArguments)
        {
            CustomDotNetCliPath = customDotNetCliPath;
            JavaScriptEngine = javaScriptEngine;
            JavaScriptEngineArguments = javaScriptEngineArguments;
            RuntimeJavaScriptName = "runtime.js";
            ExtraRuntimeArguments = "";
        }

        private string CustomDotNetCliPath { get; }

        private string JavaScriptEngine { get; set; }

        private string JavaScriptEngineArguments { get; set; }

        private string RuntimeJavaScriptName { get; set; }

        private string ExtraRuntimeArguments { get; set; }

        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            if (!File.Exists(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath))
            {
                executeParameters.Logger.WriteLineError($"Did not find {executeParameters.BuildResult.ArtifactsPaths.ExecutablePath}, but the folder contained:");
                foreach (var file in new DirectoryInfo(executeParameters.BuildResult.ArtifactsPaths.BinariesDirectoryPath).GetFiles("*.*"))
                    executeParameters.Logger.WriteLineError(file.Name);

                return new ExecuteResult(false, -1, default, Array.Empty<string>(), Array.Empty<string>());
            }

            try
            {
                return Execute(
                    executeParameters.BenchmarkCase,
                    executeParameters.BenchmarkId,
                    executeParameters.Logger,
                    executeParameters.BuildResult.ArtifactsPaths,
                    executeParameters.Diagnoser,
                    Path.GetFileName(executeParameters.BuildResult.ArtifactsPaths.ExecutablePath),
                    executeParameters.Resolver);
            }
            finally
            {
                executeParameters.Diagnoser?.Handle(
                    HostSignal.AfterProcessExit,
                    new DiagnoserActionParameters(null, executeParameters.BenchmarkCase, executeParameters.BenchmarkId));
            }
        }

        private ExecuteResult Execute(BenchmarkCase benchmarkCase,
                                      BenchmarkId benchmarkId,
                                      ILogger logger,
                                      ArtifactsPaths artifactsPaths,
                                      IDiagnoser diagnoser,
                                      string executableName,
                                      IResolver resolver)
        {
            ProcessStartInfo startInfo = WasmExecutor.BuildStartInfo(
                customDotNetCliPath: CustomDotNetCliPath,
                workingDirectory: artifactsPaths.BinariesDirectoryPath,
                programName: artifactsPaths.ProgramName,
                benchmarkId: benchmarkId.ToArguments(),
                javaScriptEngine: JavaScriptEngine,
                runtimeJavaScriptName: RuntimeJavaScriptName,
                javaScriptEngineArguments: JavaScriptEngineArguments,
                extraRuntimeArguments: ExtraRuntimeArguments,
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


        internal static ProcessStartInfo BuildStartInfo(string customDotNetCliPath,
                                                        string workingDirectory,
                                                        string programName,
                                                        string benchmarkId,
                                                        string javaScriptEngine,
                                                        string runtimeJavaScriptName,
                                                        string javaScriptEngineArguments,
                                                        string extraRuntimeArguments,
                                                        IReadOnlyList<EnvironmentVariable> environmentVariables = null,
                                                        bool redirectStandardInput = false)
        {
            const string dotnetMultiLevelLookupEnvVarName = "DOTNET_MULTILEVEL_LOOKUP";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = javaScriptEngine,
                WorkingDirectory = workingDirectory,
                Arguments = $"{javaScriptEngineArguments} {runtimeJavaScriptName} -- {extraRuntimeArguments} --run {programName}.dll {benchmarkId} ",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = redirectStandardInput
            };

            if (environmentVariables != null)
                foreach (var environmentVariable in environmentVariables)
                    startInfo.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;

            if (!string.IsNullOrEmpty(customDotNetCliPath) && (environmentVariables == null || environmentVariables.All(envVar => envVar.Key != dotnetMultiLevelLookupEnvVarName)))
                startInfo.EnvironmentVariables[dotnetMultiLevelLookupEnvVarName] = "0";

            return startInfo;
        }
    }
}