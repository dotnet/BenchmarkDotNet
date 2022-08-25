using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System;

namespace BenchmarkDotNet.Toolchains.Parameters
{
    public class ExecuteParameters
    {
        internal static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(2);

        public ExecuteParameters(BuildResult buildResult, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, IResolver resolver, int launchIndex, IDiagnoser diagnoser = null)
        {
            BuildResult = buildResult;
            BenchmarkCase = benchmarkCase;
            BenchmarkId = benchmarkId;
            Logger = logger;
            Resolver = resolver;
            Diagnoser = diagnoser;
            LaunchIndex = launchIndex;
        }

        public BuildResult BuildResult { get;  }

        public BenchmarkCase BenchmarkCase { get; }

        public BenchmarkId BenchmarkId { get; }

        public ILogger Logger { get; }

        public IResolver Resolver { get; }

        public IDiagnoser Diagnoser { get; }

        public int LaunchIndex { get; }
    }
}