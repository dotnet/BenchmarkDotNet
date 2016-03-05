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

        private string OutputDirectory { get; } = "binaries";

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

            if (!ExecuteCommand(
                $"build --framework {Framework} --configuration {Configuration} --output {OutputDirectory}", 
                generateResult.DirectoryPath, 
                logger))
            {
                // dotnet cli could have succesfully builded the program, but returned 1 as exit code because it had some warnings
                // so we need to check whether the exe exists or not, if it does then it is OK
                var executablePath = BuildExecutablePath(generateResult, benchmark);
                if (File.Exists(executablePath))
                {
                    return new BuildResult(generateResult, true, null, executablePath);
                }

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
                FileName = "dotnet",
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
        /// we use custom output path in order to avoid any future problems related to dotnet cli paths changing
        /// </summary>
        private string BuildExecutablePath(GenerateResult generateResult, Benchmark benchmark)
            => Path.Combine(generateResult.DirectoryPath, OutputDirectory, $"{benchmark.ShortInfo}.exe");
    }
}