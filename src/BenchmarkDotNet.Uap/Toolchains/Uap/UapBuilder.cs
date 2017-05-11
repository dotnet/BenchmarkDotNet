#if !UAP
using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System.Diagnostics;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Uap
{
    internal class UapBuilder : IBuilder
    {
        private const string Configuration = "Release";

        internal const string OutputDirectory = "binaries";

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
        
        public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark,
            IResolver resolver)
        {
            return ExecuteCommand("cmd", "/c \"" + generateResult.ArtifactsPaths.BuildScriptFilePath + "\"",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                DefaultTimeout, generateResult);
        }

        internal static BuildResult ExecuteCommand(string command, string arguments, string workingDirectory, 
            TimeSpan timeout, GenerateResult generateResult)
        {
            using (var process = new Process { StartInfo = BuildStartInfo(workingDirectory, command, arguments) })
            {
                process.Start();

                process.WaitForExit((int)timeout.TotalMilliseconds);

                var error = process.StandardError.ReadToEnd();

                return process.ExitCode <= 0 ? BuildResult.Success(generateResult) : BuildResult.Failure(generateResult, new Exception(error));
            }
        }

        private static ProcessStartInfo BuildStartInfo(string workingDirectory, string command, string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = command,
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
        }
    }
}
#endif