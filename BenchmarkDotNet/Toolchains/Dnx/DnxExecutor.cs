using System.Text;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
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
            var args = BuildParameters(benchmark.Job.Platform);

            return Execute(benchmark, logger, "cmd.exe", DnxGenerator.GetDirectoryPath(), args, compositeDiagnoser);
        }

        private string BuildParameters(Platform platform)
        {
            var builder = new StringBuilder("/c ");

            if (platform == Platform.AnyCpu)
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

            return builder.ToString();
        }
    }
}