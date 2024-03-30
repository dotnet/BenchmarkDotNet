using System;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using JetBrains.Profiler.SelfApi;

namespace BenchmarkDotNet.Diagnostics.dotMemory
{
    internal class InProcessDotMemoryTool : DotMemoryToolBase
    {
        public InProcessDotMemoryTool(ILogger logger, Uri? nugetUrl = null, NuGetApi nugetApi = NuGetApi.V3, string? downloadTo = null) :
            base(logger, nugetUrl, nugetApi, downloadTo) { }

        protected override void Attach(DiagnoserActionParameters parameters, string snapshotFile)
        {
            var config = new DotMemory.Config();
            config.SaveToFile(snapshotFile);
            DotMemory.Attach(config);
        }

        protected override void Snapshot(DiagnoserActionParameters parameters) => DotMemory.GetSnapshot();

        protected override void Detach() => DotMemory.Detach();
    }
}