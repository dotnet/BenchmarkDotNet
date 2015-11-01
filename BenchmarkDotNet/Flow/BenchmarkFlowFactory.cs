using System;
using BenchmarkDotNet.Flow.Classic;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Flow
{
    // TODO: add DNX support
    internal static class BenchmarkFlowFactory
    {
        public static IBenchmarkFlow CreateFlow(Benchmark benchmark, IBenchmarkLogger logger)
        {
            switch (benchmark.Task.Configuration.Executor)
            {
                case BenchmarkExecutor.Classic:
                    return new BenchmarkClassicFlow(benchmark, logger);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}