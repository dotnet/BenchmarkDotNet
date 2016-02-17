using System;
using System.Diagnostics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Dnx
{
    public class DnxBuilder : IBuilder
    {
        private static readonly int DefaultTimeout = (int)TimeSpan.FromMinutes(2).TotalMilliseconds;

        /// <summary>
        /// generates project.lock.json that tells compiler where to take dlls and source from
        /// and builds executable and copies all required dll's
        /// </summary>
        public BuildResult Build(GenerateResult generateResult, ILogger logger)
        {
            if (!ExecuteCommand("restore", generateResult.DirectoryPath, logger))
            {
                return new BuildResult(generateResult, false, new Exception("dotnet restore has failed"));
            }

            if (!ExecuteCommand("build --framework dnx451 --configuration RELEASE", generateResult.DirectoryPath, logger))
            {
                return new BuildResult(generateResult, false, new Exception("dotnet build has failed"));
            }

            return new BuildResult(generateResult, true, null);
        }

        private bool ExecuteCommand(string arguments, string workingDirectory, ILogger logger)
        {
            using (var process = new Process { StartInfo = BuildStartInfo(workingDirectory, arguments)})
            {
                using (new ProcessOutputLogger(logger, process))
                {
                    process.Start();

                    // don't forget to call, otherwise logger will not get any events
                    process.BeginErrorReadLine();
#if DEBUG
                    process.BeginOutputReadLine();
#endif

                    process.WaitForExit(DefaultTimeout);

                    return process.ExitCode <= 0;
                }
            }
        }

        private static ProcessStartInfo BuildStartInfo(string workingDirectory, string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = "dotnet.exe",
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
#if DEBUG
                RedirectStandardOutput = true,
#endif
                RedirectStandardError = true
            };
        }
    }
}