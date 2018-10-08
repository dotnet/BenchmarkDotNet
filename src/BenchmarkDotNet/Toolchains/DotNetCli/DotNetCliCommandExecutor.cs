using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public static class DotNetCliCommandExecutor
    {
        public struct CommandResult
        {
            [PublicAPI] public bool IsSuccess { get; }

            [PublicAPI] public TimeSpan ExecutionTime { get; }

            [PublicAPI] public string StandardOutput { get; }

            [PublicAPI] public string StandardError { get; }

            /// <summary>
            /// in theory, all errors should be reported to standard error, 
            /// but sometimes they are not so we can at least return 
            /// standard output which hopefully will contain some useful information
            /// </summary>
            public string ProblemDescription => HasNonEmptyErrorMessage ? StandardError : StandardOutput;

            [PublicAPI] public bool HasNonEmptyErrorMessage => !string.IsNullOrEmpty(StandardError);

            private CommandResult(bool isSuccess, TimeSpan executionTime, string standardOutput, string standardError)
            {
                IsSuccess = isSuccess;
                ExecutionTime = executionTime;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            public static CommandResult Success(TimeSpan time, string standardOutput)
                => new CommandResult(true, time, standardOutput, string.Empty);

            public static CommandResult Failure(TimeSpan time, string standardError, string standardOutput)
                => new CommandResult(false, time, standardOutput, standardError);
        }

        [PublicAPI]
        public static CommandResult ExecuteCommand(
            string customDotNetCliPath, string commandWithArguments, string workingDirectory, ILogger logger, 
            IReadOnlyList<EnvironmentVariable> environmentVariables = null, bool useSharedCompilation = false)
        {
            commandWithArguments = $"{commandWithArguments} /p:UseSharedCompilation={useSharedCompilation.ToString().ToLowerInvariant()}";

            using (var process = new Process { StartInfo = BuildStartInfo(customDotNetCliPath, workingDirectory, commandWithArguments, environmentVariables) })
            {
                var stopwatch = Stopwatch.StartNew();
                process.Start();

                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();

                process.WaitForExit();
                stopwatch.Stop();

                logger.WriteLineInfo($"// {commandWithArguments} took {stopwatch.Elapsed.TotalSeconds:0.##}s and exited with {process.ExitCode}");

                return process.ExitCode <= 0
                    ? CommandResult.Success(stopwatch.Elapsed, standardOutput)
                    : CommandResult.Failure(stopwatch.Elapsed, standardError, standardOutput);
            }
        }

        internal static string GetDotNetSdkVersion()
        {
            using (var process = new Process { StartInfo = BuildStartInfo(customDotNetCliPath: null, workingDirectory: string.Empty, arguments: "--version") })
            {
                try
                {
                    process.Start();
                }
                catch (Win32Exception) // dotnet cli is not installed
                {
                    return null;
                }

                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                // first line contains something like ".NET Command Line Tools (1.0.0-beta-001603)"
                return Regex.Split(output, Environment.NewLine, RegexOptions.Compiled)
                    .FirstOrDefault(line => !string.IsNullOrEmpty(line));
            }
        }

        internal static ProcessStartInfo BuildStartInfo(string customDotNetCliPath, string workingDirectory, string arguments,
            IReadOnlyList<EnvironmentVariable> environmentVariables = null, bool redirectStandardInput = false)
        {
            const string dotnetMultiLevelLookupEnvVarName = "DOTNET_MULTILEVEL_LOOKUP";

            var startInfo = new ProcessStartInfo
            {
                FileName = customDotNetCliPath ?? "dotnet",
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
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