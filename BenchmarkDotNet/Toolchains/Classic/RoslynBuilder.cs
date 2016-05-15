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
            var buildScriptName = generateResult.ProgramName + "-" + GeneratorBase.BuildBenchmarkScriptFileName;
            var buildScriptPath = Path.Combine(generateResult.DirectoryPath, buildScriptName);
            logger.WriteLineInfo("BuildScript: " + buildScriptPath);
            var exeFilePath = generateResult.ProgramName + ".exe";
            var buildProcess = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = generateResult.DirectoryPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            if (RuntimeInformation.IsWindows())
            {
                buildProcess.StartInfo.FileName = buildScriptPath;
            }
            else
            {
                buildProcess.StartInfo.FileName = "/bin/sh";
                buildProcess.StartInfo.Arguments = buildScriptPath;
            }
            buildProcess.Start();
            var output = buildProcess.StandardOutput.ReadToEnd();
            buildProcess.WaitForExit();
            logger.WriteLineInfo(output);

            return new BuildResult(generateResult, File.Exists(exeFilePath), null, exeFilePath);
        }
    }
}
#endif