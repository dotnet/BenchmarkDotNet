using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.Toolchains.Classic;
using BenchmarkDotNet.Plugins.Toolchains.Results;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Plugins.Toolchains.Dnx
{
    internal class BenchmarkDnxExecutor : BenchmarkClassicExecutor
    {
        public BenchmarkDnxExecutor(Benchmark benchmark, IBenchmarkLogger logger) : base(benchmark, logger)
        {
        }

        public override BenchmarkExecResult Execute(BenchmarkBuildResult buildResult, BenchmarkParameters parameters, IBenchmarkDiagnoser diagnoser)
        {
            var args = parameters == null ? "/c dnx run" : $"/c dnx run {parameters.ToArgs()}";

            return Execute("cmd.exe", BenchmarkDnxGenerator.GetDirectoryPath(), args, diagnoser);
        }
    }
}