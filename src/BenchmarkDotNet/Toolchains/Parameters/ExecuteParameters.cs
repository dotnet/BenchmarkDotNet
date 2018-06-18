using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Parameters
{
    public class ExecuteParameters
    {
        public ExecuteParameters(BuildResult buildResult, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, IResolver resolver, IConfig config, IDiagnoser diagnoser = null)
        {
            BuildResult = buildResult;
            BenchmarkCase = benchmarkCase;
            BenchmarkId = benchmarkId;
            Logger = logger;
            Resolver = resolver;
            Config = config;
            Diagnoser = diagnoser;
        }

        public BuildResult BuildResult { get;  }

        public BenchmarkCase BenchmarkCase { get; }

        public BenchmarkId BenchmarkId { get; }

        public ILogger Logger { get; }

        public IResolver Resolver { get; }

        public IConfig Config { get; }

        public IDiagnoser Diagnoser { get; }
    }
}