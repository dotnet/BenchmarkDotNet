using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Toolchains.DotNetCli;
using System.IO;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    public class WasmBuilder : DotNetCliBuilder
    {
        private WasmAppBuilder WasmAppBuilder;

        public WasmBuilder(string targetFrameworkMoniker, WasmSettings wasmSettings, string customDotNetCliPath = null, TimeSpan? timeout = null)
            :base (targetFrameworkMoniker, customDotNetCliPath, timeout)
        {
            WasmAppBuilder = new WasmAppBuilder(wasmSettings, targetFrameworkMoniker);
        }

        public override BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            BuildResult buildResult = base.Build(generateResult, buildPartition, logger);
            WasmAppBuilder.BuildApp(buildPartition.ProgramName, generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);

            return buildResult;
        }
    }
}
