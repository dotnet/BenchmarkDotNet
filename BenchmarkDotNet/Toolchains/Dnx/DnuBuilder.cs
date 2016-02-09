using System;
using System.Diagnostics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Dnx
{
    /// <summary>
    /// relies on a MS "dnu" command line tool (it is just Microsoft.Dnx.Tooling.dll installed with dnvm)
    /// Nuget 3 will replace dnu restore in the future: https://github.com/aspnet/dnx/issues/3216
    /// </summary>
    public class DnuBuilder : IBuilder
    {
        private static readonly int DefaultTimeout = (int)TimeSpan.FromMinutes(2).TotalMilliseconds;

        /// <summary>
        /// generates project.lock.json that tells compiler where to take dlls and source from
        /// </summary>
        public BuildResult Build(GenerateResult generateResult, ILogger logger)
        {
            if (!ExecuteCommand(generateResult.DirectoryPath, "dnu restore", logger))
            {
                return new BuildResult(generateResult, true, new Exception("dnu restore has failed"));
            }

            return new BuildResult(generateResult, true, null);
        }

        private bool ExecuteCommand(string workingDirectory, string arguments, ILogger logger)
        {
            using (var process = new Process { StartInfo = BuildStartInfo(workingDirectory, arguments)})
            {
                using (new ProcessOutputLogger(logger, process))
                {
                    process.Start();

                    // don't forget to call, otherwise logger will not get any events
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();

                    process.WaitForExit(DefaultTimeout);

                    return process.ExitCode <= 0;
                }
            }
        }

        private static ProcessStartInfo BuildStartInfo(string workingDirectory, string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = workingDirectory,
                Arguments = $"/c {arguments}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
        }
    }
}