using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public static class DotNetCliCommandExecutor
    {
        internal static readonly Lazy<string> DefaultDotNetCliPath = new Lazy<string>(GetDefaultDotNetCliPath);

        [PublicAPI]
        public static DotNetCliCommandResult Execute(DotNetCliCommand parameters)
        {
            using (var process = new Process { StartInfo = BuildStartInfo(parameters.CliPath, parameters.GenerateResult?.ArtifactsPaths.BuildArtifactsDirectoryPath, parameters.Arguments, parameters.EnvironmentVariables) })
            using (var outputReader = new AsyncProcessOutputReader(process, parameters.LogOutput, parameters.Logger))
            using (new ConsoleExitHandler(process, parameters.Logger))
            {
                parameters.Logger.WriteLineInfo($"// start {process.StartInfo.FileName} {process.StartInfo.Arguments} in {process.StartInfo.WorkingDirectory}");

                var stopwatch = Stopwatch.StartNew();

                process.Start();
                outputReader.BeginRead();

                if (!process.WaitForExit((int)parameters.Timeout.TotalMilliseconds))
                {
                    parameters.Logger.WriteLineError($"// command took longer than the timeout: {parameters.Timeout.TotalSeconds:0.##}s. Killing the process tree!");

                    outputReader.CancelRead();
                    process.KillTree();

                    return DotNetCliCommandResult.Failure(stopwatch.Elapsed, $"The configured timeout {parameters.Timeout} was reached!" + outputReader.GetErrorText(), outputReader.GetOutputText());
                }

                stopwatch.Stop();
                outputReader.StopRead();

                parameters.Logger.WriteLineInfo($"// command took {stopwatch.Elapsed.TotalSeconds:0.##}s and exited with {process.ExitCode}");

                return process.ExitCode <= 0
                    ? DotNetCliCommandResult.Success(stopwatch.Elapsed, outputReader.GetOutputText())
                    : DotNetCliCommandResult.Failure(stopwatch.Elapsed, outputReader.GetOutputText(), outputReader.GetErrorText());
            }
        }

        internal static string GetDotNetSdkVersion()
        {
            using (var process = new Process { StartInfo = BuildStartInfo(customDotNetCliPath: null, workingDirectory: string.Empty, arguments: "--version") })
            using (new ConsoleExitHandler(process, NullLogger.Instance))
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

        internal static void LogEnvVars(DotNetCliCommand command)
        {
            if (!command.LogOutput)
            {
                return;
            }

            ProcessStartInfo startInfo = BuildStartInfo(
                command.CliPath, command.GenerateResult.ArtifactsPaths.BuildArtifactsDirectoryPath, command.Arguments, command.EnvironmentVariables);

            if (startInfo.EnvironmentVariables.Keys.Count > 0)
            {
                command.Logger.WriteLineInfo("// Environment Variables:");
                foreach (string name in startInfo.EnvironmentVariables.Keys)
                {
                    command.Logger.WriteLine($"\t[{name}] = \"{startInfo.EnvironmentVariables[name]}\"");
                }
            }
        }

        internal static ProcessStartInfo BuildStartInfo(string customDotNetCliPath, string workingDirectory, string arguments,
            IReadOnlyList<EnvironmentVariable> environmentVariables = null, bool redirectStandardInput = false, bool redirectStandardError = true, bool redirectStandardOutput = true)
        {
            const string dotnetMultiLevelLookupEnvVarName = "DOTNET_MULTILEVEL_LOOKUP";

            var startInfo = new ProcessStartInfo
            {
                FileName = customDotNetCliPath ?? DefaultDotNetCliPath.Value,
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = redirectStandardOutput,
                RedirectStandardError = redirectStandardError,
                RedirectStandardInput = redirectStandardInput,
            };

            if (redirectStandardOutput)
            {
                startInfo.StandardOutputEncoding = Encoding.UTF8;
            }

            if (redirectStandardError) // StandardErrorEncoding is only supported when standard error is redirected
            {
                startInfo.StandardErrorEncoding = Encoding.UTF8;
            }

            if (environmentVariables != null)
                foreach (var environmentVariable in environmentVariables)
                    startInfo.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;

            if (!string.IsNullOrEmpty(customDotNetCliPath) && (environmentVariables == null || environmentVariables.All(envVar => envVar.Key != dotnetMultiLevelLookupEnvVarName)))
                startInfo.EnvironmentVariables[dotnetMultiLevelLookupEnvVarName] = "0";

            return startInfo;
        }

        private static string GetDefaultDotNetCliPath()
        {
            if (!Portability.RuntimeInformation.IsLinux())
                return "dotnet";

            using (var parentProcess = Process.GetProcessById(libc.getppid()))
            {
                string parentPath = parentProcess.MainModule?.FileName ?? string.Empty;
                // sth like /snap/dotnet-sdk/112/dotnet and we should use the exact path instead of just "dotnet"
                if (parentPath.StartsWith("/snap/", StringComparison.Ordinal) &&
                    parentPath.EndsWith("/dotnet", StringComparison.Ordinal))
                {
                    return parentPath;
                }

                return "dotnet";
            }
        }

        internal static string GetSdkPath(string cliPath)
        {
            DotNetCliCommand cliCommand = new (
                cliPath: cliPath,
                arguments: "--info",
                generateResult: null,
                logger: NullLogger.Instance,
                buildPartition: null,
                environmentVariables: Array.Empty<EnvironmentVariable>(),
                timeout: TimeSpan.FromMinutes(1),
                logOutput: false);

            string sdkPath = Execute(cliCommand)
                .StandardOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.EndsWith("/sdk]")) // sth like "  3.1.423 [/usr/share/dotnet/sdk]
                .Select(line => line.Split('[')[1])
                .Distinct()
                .Single(); // I assume there will be only one such folder

            return sdkPath.Substring(0, sdkPath.Length - 1); // remove trailing `]`
        }
    }
}