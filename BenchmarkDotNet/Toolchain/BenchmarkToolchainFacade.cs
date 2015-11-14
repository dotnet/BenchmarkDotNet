using System;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Toolchain.Classic;
using BenchmarkDotNet.Toolchain.Results;

namespace BenchmarkDotNet.Toolchain
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

        public BenchmarkExecResult Exec(BenchmarkBuildResult buildResult, BenchmarkParameters parameters)
        {
            return executor.Exec(buildResult, parameters);
        }

        public static IBenchmarkToolchainFacade CreateToolchain(Benchmark benchmark, IBenchmarkLogger logger)
        {
            switch (benchmark.Task.Configuration.Toolchain)
            {
                case BenchmarkToolchain.Classic:
                    return new BenchmarkToolchainFacade(benchmark, new BenchmarkClassicGenerator(logger), new BenchmarkClassicBuilder(logger), new BenchmarkClassicExecutor(benchmark, logger));
                default:
                    throw new NotSupportedException();
            }
        }
    }
}