using BenchmarkDotNet.Flow.Results;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Flow.Classic
{
    internal class BenchmarkClassicFlow : IBenchmarkFlow
    {
        private readonly Benchmark benchmark;

        private readonly BenchmarkClassicGenerator generator;
        private readonly BenchmarkClassicBuilder builder;
        private readonly BenchmarkClassicExecutor executor;

        public BenchmarkClassicFlow(Benchmark benchmark, IBenchmarkLogger logger)
        {
            this.benchmark = benchmark;
            generator = new BenchmarkClassicGenerator();
            builder = new BenchmarkClassicBuilder(logger);
            executor = new BenchmarkClassicExecutor(benchmark, logger);
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
    }
}