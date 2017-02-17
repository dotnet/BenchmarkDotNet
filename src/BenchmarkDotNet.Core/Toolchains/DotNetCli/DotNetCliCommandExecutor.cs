using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Loggers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public class DotNetCliCommandExecutor
    {
        [PublicAPI]
        public static bool ExecuteCommand(string commandWithArguments, string workingDirectory, ILogger logger, TimeSpan timeout)
        {
            using (var process = new Process { StartInfo = BuildStartInfo(workingDirectory, commandWithArguments) })
            {
                using (new AsyncErrorOutputLogger(logger, process))
                {
                    process.Start();

                    // don't forget to call, otherwise logger will not get any events
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();

                    process.WaitForExit((int)timeout.TotalMilliseconds);

                    return process.ExitCode <= 0;
                }
            }
        }

        internal static string GetDotNetCliVersion()
        {
            using (var process = new Process { StartInfo = BuildStartInfo(arguments: "--version", workingDirectory: string.Empty) })
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

        internal static ProcessStartInfo BuildStartInfo(string workingDirectory, string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
        }
    }
}