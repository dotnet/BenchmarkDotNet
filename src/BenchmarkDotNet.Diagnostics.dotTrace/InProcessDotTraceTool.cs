using System;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using JetBrains.Profiler.SelfApi;

namespace BenchmarkDotNet.Diagnostics.dotTrace
{
    internal class InProcessDotTraceTool : DotTraceToolBase
    {
        public InProcessDotTraceTool(ILogger logger, Uri? nugetUrl = null, NuGetApi nugetApi = NuGetApi.V3, string? downloadTo = null) :
            base(logger, nugetUrl, nugetApi, downloadTo) { }

        protected override bool AttachOnly => false;

        protected override void Attach(DiagnoserActionParameters parameters, string snapshotFile)
        {
            var config = new DotTrace.Config();
            config.SaveToFile(snapshotFile);
            DotTrace.Attach(config);
        }

        protected override void StartCollectingData() => DotTrace.StartCollectingData();

        protected override void SaveData() => DotTrace.SaveData();

        protected override void Detach() => DotTrace.Detach();
    }
}