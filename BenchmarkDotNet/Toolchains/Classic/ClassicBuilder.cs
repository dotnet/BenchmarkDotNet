using System.Collections.Generic;
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
                var consoleLogger = new MsBuildConsoleLogger(logger);
                var globalProperties = new Dictionary<string, string>();
                var buildRequest = new BuildRequestData(projectFileName, globalProperties, null, new[] { "Build" }, null);
                var buildParameters = new BuildParameters(new ProjectCollection()) { DetailedSummary = false, Loggers = new Microsoft.Build.Framework.ILogger[] { consoleLogger } };
                var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
                return new BuildResult(generateResult, buildResult.OverallResult == BuildResultCode.Success, buildResult.Exception);
            }
        }
    }
}