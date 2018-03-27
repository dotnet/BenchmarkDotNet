using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    internal class DotNetCliCommandExecutor
    {
        internal struct CommandResult
        {
            public bool IsSuccess { get; }

            public TimeSpan ExecutionTime { get; }

            public string StandardOutput { get; }

            public string StandardError { get; }

            /// <summary>
            /// in theory, all errors should be reported to standard error, 
            /// but sometimes they are not so we can at least return 
            /// standard output which hopefully will contain some useful information
            /// </summary>
            public string ProblemDescription => HasNonEmptyErrorMessage ? StandardError : StandardOutput;

            public bool HasNonEmptyErrorMessage => !string.IsNullOrEmpty(StandardError);

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

        internal static CommandResult ExecuteCommand(
            string customDotNetCliPath, string commandWithArguments, string workingDirectory, ILogger logger, bool useSharedCompilation = false)
        {
            commandWithArguments = $"{commandWithArguments} /p:UseSharedCompilation={useSharedCompilation.ToString().ToLowerInvariant()}";

            using (var process = new Process { StartInfo = BuildStartInfo(customDotNetCliPath, workingDirectory, commandWithArguments) })
            {
                var stopwatch = Stopwatch.StartNew();
                process.Start();

                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = process.StandardError.ReadToEnd();

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
            using (var process = new Process { StartInfo = BuildStartInfo(arguments: "--version", workingDirectory: string.Empty, customDotNetCliPath: null) })
            {
                try
                {
                    process.Start();
                }
                catch (Win32Exception) // dotnet cli is not installed
                {
                    return null;
                }

                var output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                // first line contains something like ".NET Command Line Tools (1.0.0-beta-001603)"
                return Regex.Split(output, System.Environment.NewLine, RegexOptions.Compiled)
                    .FirstOrDefault(line => !string.IsNullOrEmpty(line));
            }
        }

        internal static ProcessStartInfo BuildStartInfo(string customDotNetCliPath, string workingDirectory, string arguments, bool redirectStandardInput = false) 
            => new ProcessStartInfo
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
    }
}