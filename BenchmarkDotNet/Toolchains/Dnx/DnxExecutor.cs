using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Classic;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Dnx
{
    internal class DnxExecutor : ClassicExecutor
    {
        public override ExecuteResult Execute(
            BuildResult buildResult,
            Benchmark benchmark,
            ILogger logger,
            IDiagnoser compositeDiagnoser = null)
        {
            var args = "run --framework dnx451 --configuration RELEASE";

            return Execute(benchmark, logger, "dotnet.exe", DnxGenerator.GetDirectoryPath(), args, compositeDiagnoser);
        }
    }
}