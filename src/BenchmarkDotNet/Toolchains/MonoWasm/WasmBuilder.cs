using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Toolchains.DotNetCli;
using System.IO;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    public class WasmBuilder : IBuilder
    {
        private readonly DotNetCliBuilder dotNetCliBuilder;
        private readonly string targetFrameworkMoniker;

        public WasmBuilder(string targetFrameworkMoniker, string customDotNetCliPath = null, TimeSpan? timeout = null)
        {
            this.targetFrameworkMoniker = targetFrameworkMoniker;

            dotNetCliBuilder = new DotNetCliBuilder(targetFrameworkMoniker, customDotNetCliPath, timeout);
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            BuildResult buildResult = dotNetCliBuilder.Build(generateResult, buildPartition, logger);

            WasmRuntime runtime = (WasmRuntime)buildPartition.Runtime;

            return buildResult;
        }

    }
}
