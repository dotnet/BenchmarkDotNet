using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System;

namespace BenchmarkDotNet.Toolchains.Parameters
{
    public class ExecuteParameters(BuildResult buildResult, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId,
        ILogger logger, IResolver resolver, int launchIndex,
        CompositeInProcessDiagnoser compositeInProcessDiagnoser,
        IDiagnoser? diagnoser = null, RunMode diagnoserRunMode = RunMode.None)
    {
        internal static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(2);

        public BuildResult BuildResult { get; } = buildResult;

        public BenchmarkCase BenchmarkCase { get; } = benchmarkCase;

        public BenchmarkId BenchmarkId { get; } = benchmarkId;

        public ILogger Logger { get; } = logger;

        public IResolver Resolver { get; } = resolver;

        public CompositeInProcessDiagnoser CompositeInProcessDiagnoser { get; } = compositeInProcessDiagnoser;

        public IDiagnoser? Diagnoser { get; } = diagnoser;

        public int LaunchIndex { get; } = launchIndex;

        public RunMode DiagnoserRunMode { get; } = diagnoserRunMode;
    }
}