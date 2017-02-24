using System;
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
        public static string RunAndReadOutput(string fileName, string arguments)
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
    }
}