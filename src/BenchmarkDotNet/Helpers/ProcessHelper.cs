using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using BenchmarkDotNet.Loggers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Helpers
{
    internal static class ProcessHelper
    {
        /// <summary>
        /// Run external process and return the console output.
        /// In the case of any exception, null will be returned.
        /// </summary>
        [CanBeNull]
        internal static string RunAndReadOutput(string fileName, string arguments = "", ILogger logger = null)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = "",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
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
            Dictionary<string, string> environmentVariables = null, bool includeErrors = false, ILogger logger = null)
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

            if (environmentVariables != null)
                foreach (var environmentVariable in environmentVariables)
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
    }
}