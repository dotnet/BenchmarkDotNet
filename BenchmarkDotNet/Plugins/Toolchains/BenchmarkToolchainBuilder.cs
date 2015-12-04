using System;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Plugins.Toolchains
{
    public class BenchmarkToolchainBuilder : IBenchmarkToolchainBuilder
    {
        private readonly Func<Benchmark, IBenchmarkLogger, IBenchmarkGenerator> createGenerator;
        private readonly Func<Benchmark, IBenchmarkLogger, IBenchmarkBuilder> createBuilder;
        private readonly Func<Benchmark, IBenchmarkLogger, IBenchmarkExecutor> createExecutor;

        public BenchmarkToolchain TargetToolchain { get; }

        public BenchmarkToolchainBuilder(BenchmarkToolchain targetToolchain, Func<Benchmark, IBenchmarkLogger, IBenchmarkGenerator> createGenerator, Func<Benchmark, IBenchmarkLogger, IBenchmarkBuilder> createBuilder, Func<Benchmark, IBenchmarkLogger, IBenchmarkExecutor> createExecutor)
        {
            this.createGenerator = createGenerator;
            this.createBuilder = createBuilder;
            this.createExecutor = createExecutor;
            TargetToolchain = targetToolchain;
        }

        public IBenchmarkToolchainFacade Build(Benchmark benchmark, IBenchmarkLogger logger)
        {
            return new BenchmarkToolchainFacade(benchmark, createGenerator(benchmark, logger), createBuilder(benchmark, logger), createExecutor(benchmark, logger));
        }
    }
}