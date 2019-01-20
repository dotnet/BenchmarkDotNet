using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        internal static string RunAndReadOutput(string fileName, string arguments = "")
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

        internal static (int exitCode, IReadOnlyList<string> output) RunAndReadOutputLineByLine(string fileName, string arguments = "", string workingDirectory = "",
            Dictionary<string, string> environmentVariables = null, bool includeErrors = false)
        {
            var output = new List<string>(20000);

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

            if(environmentVariables != null)
                foreach (var environmentVariable in environmentVariables)
                    processStartInfo.Environment[environmentVariable.Key] = environmentVariable.Value;

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.OutputDataReceived += (sender, args) => output.Add(args.Data);
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (includeErrors)
                        output.Add(args.Data);
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                return (process.ExitCode, output);
            }
        }
    }
}