using System.Text;
using BenchmarkDotNet.Extensions;
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
            var args = BuildParameters(parameters, benchmark.Task.Configuration.Platform);

            return Execute("cmd.exe", BenchmarkDnxGenerator.GetDirectoryPath(), args, diagnoser);
        }

        private string BuildParameters(BenchmarkParameters benchmarkParameters, BenchmarkPlatform platform)
        {
            var builder = new StringBuilder("/c ");

            if (platform == BenchmarkPlatform.AnyCpu)
            {
                builder.Append("dnx run ");
            }
            else
            {
                builder.AppendFormat("dnvm run default run -arch {0} ", platform.ToConfig());
            }

#if DNX451
            builder.Append("-r clr ");
#elif CORECLR
            builder.Append("-r coreclr ");
#endif

            if (benchmarkParameters != null)
            {
                builder.Append(benchmarkParameters.ToArgs());
            }

            return builder.ToString();
        }
    }
}