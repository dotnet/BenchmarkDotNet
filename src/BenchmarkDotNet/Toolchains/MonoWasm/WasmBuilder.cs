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
    public class WasmBuilder : IBuilder
    {
        private readonly DotNetCliBuilder dotNetCliBuilder;
        private readonly WasmAppBuilder wasmAppBuilder;

        public WasmBuilder(string targetFrameworkMoniker, WasmSettings wasmSettings, string customDotNetCliPath = null, TimeSpan? timeout = null)
        {
            dotNetCliBuilder = new DotNetCliBuilder(targetFrameworkMoniker, customDotNetCliPath, timeout);
            wasmAppBuilder = new WasmAppBuilder(wasmSettings, targetFrameworkMoniker);
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            BuildResult buildResult = dotNetCliBuilder.Build(generateResult, buildPartition, logger);

            if (buildResult.IsBuildSuccess)
            {
                wasmAppBuilder.BuildApp(buildPartition.ProgramName, generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);
            }

            return buildResult;
        }
    }
}
