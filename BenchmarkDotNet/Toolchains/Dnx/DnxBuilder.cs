using System;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Dnx
{
    public class DnxBuilder : IBuilder
    {
        private static readonly int DefaultTimeout = (int)TimeSpan.FromMinutes(2).TotalMilliseconds;

        private string Framework { get; } = "dnx451";

        private string Configuration { get; } = "RELEASE";

        /// <summary>
        /// generates project.lock.json that tells compiler where to take dlls and source from
        /// and builds executable and copies all required dll's
        /// </summary>
        public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark)
        {
            if (!ExecuteCommand("restore", generateResult.DirectoryPath, logger))
            {
                return new BuildResult(generateResult, false, new Exception("dotnet restore has failed"), null);
            }

            if (!ExecuteCommand($"build --framework {Framework} --configuration {Configuration}", generateResult.DirectoryPath, logger))
            {
                return new BuildResult(generateResult, false, new Exception("dotnet build has failed"), null);
            }

            return new BuildResult(generateResult, true, null, BuildExecutablePath(generateResult, benchmark));
        }

        private static bool ExecuteCommand(string commandWithArguments, string workingDirectory, ILogger logger)
        {
            using (var process = new Process { StartInfo = BuildStartInfo(workingDirectory, commandWithArguments)})
            {
                using (new AsynchronousProcessOutputLogger(logger, process))
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

        /// <summary>
        /// based on info from https://github.com/dotnet/cli
        /// "dotnet build will drop a binary in ./bin/[configuration]/[framework]/[binary name] that you can just run."
        /// </summary>
        private string BuildExecutablePath(GenerateResult generateResult, Benchmark benchmark)
            => Path.Combine(
                generateResult.DirectoryPath, $"bin\\{Configuration}\\{Framework}", $"{benchmark.ShortInfo}.exe");
    }
}