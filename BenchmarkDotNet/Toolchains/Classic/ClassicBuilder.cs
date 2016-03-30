#if CLASSIC
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using BuildResult = BenchmarkDotNet.Toolchains.Results.BuildResult;
using ILogger = BenchmarkDotNet.Loggers.ILogger;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class ClassicBuilder : IBuilder
    {
        public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark)
        {
            var projectFilePath = Path.Combine(generateResult.DirectoryPath, GeneratorBase.MainClassName + ".csproj");
            var executablePath = Path.Combine(generateResult.DirectoryPath, GeneratorBase.MainClassName + ".exe");

            try
            {
                var buildResult = BuildWithManagedApi(logger, projectFilePath);

                if (buildResult.OverallResult != BuildResultCode.Success && !File.Exists(executablePath))
                {
                    logger.WriteLineInfo("BuildManager.DefaultBuildManager can't build this project. =(");

                    return BuildWithBat(generateResult, logger, executablePath);
                }

                return new BuildResult(generateResult, buildResult.OverallResult == BuildResultCode.Success, buildResult.Exception, executablePath);
            }
            catch (FileNotFoundException msBuildDllNotFound)
            {
                logger.WriteLineInfo($"Unable to load {msBuildDllNotFound.FileName}");

                return BuildWithBat(generateResult, logger, executablePath);
            }
        }

        private Microsoft.Build.Execution.BuildResult BuildWithManagedApi(ILogger logger, string projectFilePath)
        {
            var consoleLogger = new MsBuildConsoleLogger(logger);
            var globalProperties = new Dictionary<string, string>();
            var buildRequest = new BuildRequestData(projectFilePath, globalProperties, null, new[] { "Build" }, null);
            var buildParameters = new BuildParameters(new ProjectCollection())
            {
                DetailedSummary = false,
                Loggers = new Microsoft.Build.Framework.ILogger[] { consoleLogger }
            };

            return BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
        }

        private BuildResult BuildWithBat(GenerateResult generateResult, ILogger logger, string exeFilePath)
        {
            logger.WriteLineInfo("Let's try to build it via BuildBenchmark.bat!");

            var buildProcess = new Process
            {
                StartInfo =
                {
                    FileName = GenerateProjectBuildFile(generateResult.DirectoryPath), // let's wait with bat generation until it is not needed
                    WorkingDirectory = generateResult.DirectoryPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                }
            };
            buildProcess.Start();
            buildProcess.WaitForExit();

            return new BuildResult(generateResult, File.Exists(exeFilePath), null, exeFilePath);
        }

        private string GenerateProjectBuildFile(string projectDir)
        {
            var content = ResourceHelper.LoadTemplate("BuildBenchmark.txt");
            string filePath = Path.Combine(projectDir, "BuildBenchmark.bat");
            File.WriteAllText(filePath, content);

            return filePath;
        }
    }
}
#endif