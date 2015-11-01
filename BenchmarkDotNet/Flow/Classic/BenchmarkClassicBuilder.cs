using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Flow.Results;
using BenchmarkDotNet.Logging;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace BenchmarkDotNet.Flow.Classic
{
    internal class BenchmarkClassicBuilder
    {
        private readonly IBenchmarkLogger logger;

        public BenchmarkClassicBuilder(IBenchmarkLogger logger)
        {
            this.logger = logger;
        }

        public BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult)
        {
            var projectFileName = Path.Combine(generateResult.DirectoryPath, BenchmarkClassicGenerator.MainClassName + ".csproj");
            var consoleLogger = new MSBuildConsoleLogger(logger);
            var globalProperties = new Dictionary<string, string>();
            var buildRequest = new BuildRequestData(projectFileName, globalProperties, null, new[] { "Build" }, null);
            var buildParameters = new BuildParameters(new ProjectCollection()) { DetailedSummary = false, Loggers = new ILogger[] { consoleLogger } };
            var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
            return new BenchmarkBuildResult(generateResult, buildResult.OverallResult == BuildResultCode.Success, buildResult.Exception);
        }
    }
}