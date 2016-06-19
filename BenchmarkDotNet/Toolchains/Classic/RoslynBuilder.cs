#if CLASSIC
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BuildResult = BenchmarkDotNet.Toolchains.Results.BuildResult;
using ILogger = BenchmarkDotNet.Loggers.ILogger;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class RoslynBuilder : IBuilder
    {
        public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark)
        {
            logger.WriteLineInfo($"BuildScript: {generateResult.ArtifactsPaths.BuildScriptFilePath}");

            var buildProcess = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            if (RuntimeInformation.IsWindows())
            {
                buildProcess.StartInfo.FileName = generateResult.ArtifactsPaths.BuildScriptFilePath;
            }
            else
            {
                buildProcess.StartInfo.FileName = "/bin/sh";
                buildProcess.StartInfo.Arguments = generateResult.ArtifactsPaths.BuildScriptFilePath;
            }
            buildProcess.Start();
            var output = buildProcess.StandardOutput.ReadToEnd();
            buildProcess.WaitForExit();
            logger.WriteLineInfo(output);

            return new BuildResult(generateResult, File.Exists(generateResult.ArtifactsPaths.ExecutablePath), null);
        }
    }
}
#endif