using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.Toolchains.Classic;

namespace BenchmarkDotNet.Plugins.Toolchains.Dnx
{
    internal class BenchmarkDnxExecutor : BenchmarkClassicExecutor
    {
        public BenchmarkDnxExecutor(Benchmark benchmark, IBenchmarkLogger logger) : base(benchmark, logger)
        {
        }

        // todo: execute our new package with dnx
    }
}