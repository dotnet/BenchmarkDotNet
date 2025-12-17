using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Helpers
{
    internal static class ProcessHelper
    {
        /// <summary>
        /// Run external process and return the console output.
        /// In the case of any exception, null will be returned.
        /// </summary>
        internal static string? RunAndReadOutput(string fileName, string arguments = "", ILogger? logger = null,
            Dictionary<string, string>? environmentVariables = null)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = "",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };

            foreach (var variable in environmentVariables ?? [])
                processStartInfo.Environment[variable.Key] = variable.Value;

            using (var process = new Process { StartInfo = processStartInfo })
            using (new ConsoleExitHandler(process, logger ?? NullLogger.Instance))
            {
                try
                {
                    process.Start();
                }
                catch (Exception)
                {
                    return null;
                }
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }

        internal static (int exitCode, ImmutableArray<string> output) RunAndReadOutputLineByLine(string fileName, string arguments = "", string workingDirectory = "",
            Dictionary<string, string>? environmentVariables = null, bool includeErrors = false, ILogger? logger = null)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var environmentVariable in environmentVariables ?? [])
                processStartInfo.Environment[environmentVariable.Key] = environmentVariable.Value;

            using (var process = new Process { StartInfo = processStartInfo })
            using (var outputReader = new AsyncProcessOutputReader(process))
            using (new ConsoleExitHandler(process, logger ?? NullLogger.Instance))
            {
                process.Start();

                outputReader.BeginRead();

                process.WaitForExit();

                outputReader.StopRead();

                var output = includeErrors ? outputReader.GetOutputAndErrorLines() : outputReader.GetOutputLines();

                return (process.ExitCode, output);
            }
        }

        internal static bool TestCommandExists(string commandName, string arguments = "--version")
        {
            // Check command existence by using where/which command.
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = OsDetector.IsWindows() ? "where" : "which",
                    Arguments = commandName,
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!;
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                // On some environment. which command is not installed. (e.g. Alpine Linux)
            }

            // Check command existence by executing actual command with --version argument.
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = commandName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!;
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryResolveExecutableInPath(string? value, [NotNullWhen(true)] out string? result)
        {
            result = value!;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (File.Exists(value))
                return true;

            // Typed to char[] because it could be a string or char[] with newer .net versions
            var directories = Environment.GetEnvironmentVariable("PATH")!
                .Split((char[])[Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries);

            if (OsDetector.IsWindows())
            {
                var extensions = Environment.GetEnvironmentVariable("PATHEXT")!
                    .Split((char[])[Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries);

                foreach (var directory in directories)
                {
                    foreach (var ext in extensions)
                    {
                        var candidate = Path.Combine(directory, value + ext);
                        if (File.Exists(candidate))
                        {
                            result = candidate;
                            return true;
                        }
                    }
                }
            }
            else
            {
                foreach (var directory in directories)
                {
                    var candidate = Path.Combine(directory, value);
                    if (File.Exists(Path.Combine(directory, value)))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}