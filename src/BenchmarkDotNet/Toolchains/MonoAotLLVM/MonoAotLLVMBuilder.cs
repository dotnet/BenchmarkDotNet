using System;
using System.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.MonoAotLLVM
{
    public class MonoAotLLVMBuilder : IBuilder
    {
        private readonly DotNetCliBuilder dotnetCliBuilder;

        public MonoAotLLVMBuilder(string targetFrameworkMoniker, string customDotNetCliPath = null, TimeSpan? timeout = null)
        {
            dotnetCliBuilder = new DotNetCliBuilder(targetFrameworkMoniker, customDotNetCliPath, timeout);
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            BuildResult buildResult = dotnetCliBuilder.Build(generateResult, buildPartition, logger);

            return buildResult;
        }

    }
}
