using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Toolchains.Results;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Plugins.Toolchains
{
    internal class BenchmarkToolchainFacade : IBenchmarkToolchainFacade
    {
        private readonly Benchmark benchmark;
        private readonly IBenchmarkGenerator generator;
        private readonly IBenchmarkBuilder builder;
        private readonly IBenchmarkExecutor executor;

        public BenchmarkToolchainFacade(Benchmark benchmark, IBenchmarkGenerator generator, IBenchmarkBuilder builder, IBenchmarkExecutor executor)
        {
            this.benchmark = benchmark;
            this.generator = generator;
            this.builder = builder;
            this.executor = executor;
        }

        public BenchmarkGenerateResult Generate()
        {
            return generator.GenerateProject(benchmark);
        }

        public BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult)
        {
            return builder.Build(generateResult);
        }

        public BenchmarkExecResult Execute(BenchmarkBuildResult buildResult, BenchmarkParameters parameters, IBenchmarkDiagnoser diagnoser)
        {
            return executor.Execute(buildResult, parameters, diagnoser);
        }
    }
}