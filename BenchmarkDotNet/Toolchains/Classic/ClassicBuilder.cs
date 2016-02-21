#if CLASSIC
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.Results;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using BuildResult = BenchmarkDotNet.Toolchains.Results.BuildResult;
using ILogger = BenchmarkDotNet.Loggers.ILogger;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class ClassicBuilder : IBuilder
    {
        private static readonly object buildLock = new object();

        public BuildResult Build(GenerateResult generateResult, ILogger logger)
        {
            lock (buildLock)
            {
                var projectFileName = Path.Combine(generateResult.DirectoryPath, ClassicGenerator.MainClassName + ".csproj");
                var exeFilePath = Path.Combine(generateResult.DirectoryPath, ClassicGenerator.MainClassName + ".exe");
                var consoleLogger = new MsBuildConsoleLogger(logger);
                var globalProperties = new Dictionary<string, string>();
                var buildRequest = new BuildRequestData(projectFileName, globalProperties, null, new[] { "Build" }, null);
                var buildParameters = new BuildParameters(new ProjectCollection()) { DetailedSummary = false, Loggers = new Microsoft.Build.Framework.ILogger[] { consoleLogger } };
                var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);

                if (buildResult.OverallResult != BuildResultCode.Success && !File.Exists(exeFilePath))
                {
                    logger.WriteLineInfo("BuildManager.DefaultBuildManager can't build this project. =(");
                    logger.WriteLineInfo("Let's try to build it via BuildBenchmark.bat!");
                    var buildProcess = new Process
                    {
                        StartInfo =
                        {
                            FileName = Path.Combine(generateResult.DirectoryPath, "BuildBenchmark.bat"),
                            WorkingDirectory = generateResult.DirectoryPath,
                            UseShellExecute = false,
                            RedirectStandardOutput = false,
                        }
                    };
                    buildProcess.Start();
                    buildProcess.WaitForExit();
                    if (File.Exists(exeFilePath))
                        return new BuildResult(generateResult, true, null, exeFilePath);
                }

                return new BuildResult(generateResult, buildResult.OverallResult == BuildResultCode.Success, buildResult.Exception, exeFilePath);
            }
        }
    }
}
#endif