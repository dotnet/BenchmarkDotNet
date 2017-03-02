#if !UAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Uap
{
    internal class UapBuilder : IBuilder
    {
        private const string Configuration = "Release";

        internal const string OutputDirectory = "binaries";

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

        public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark,
            IResolver resolver)
        {
            if (!ExecuteCommand("cmd /c \"" + generateResult.ArtifactsPaths.BuildScriptFilePath + "\"",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                DefaultTimeout))
            {
                return BuildResult.Failure(generateResult, new Exception(" failed"));
            }

            return BuildResult.Success(generateResult);
        }

        internal static bool ExecuteCommand(string commandWithArguments, string workingDirectory, TimeSpan timeout)
        {
            using (var process = new Process { StartInfo = BuildStartInfo(workingDirectory, commandWithArguments) })
            {
                process.Start();

                process.WaitForExit((int)timeout.TotalMilliseconds);

                return process.ExitCode <= 0;
            }
        }

        private static ProcessStartInfo BuildStartInfo(string workingDirectory, string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = arguments.Split(' ').First(),
                WorkingDirectory = workingDirectory,
                Arguments = string.Join(" ", arguments.Split(' ').Skip(1)),
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
    }
}
#endif