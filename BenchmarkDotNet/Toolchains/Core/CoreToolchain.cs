using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Classic;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Core
{
    public class CoreToolchain : Toolchain
    {
        private const string TargetFrameworkMoniker = "dnxcore50"; // todo: when dnx gets replaced in VS with dotnet cli replace this name with fancy dotnet5.4 name

        public static readonly IToolchain Instance = new CoreToolchain();

        private CoreToolchain()
            : base("Core",
                  new DotNetCliGenerator(TargetFrameworkMoniker),
                  new DotNetCliBuilder(TargetFrameworkMoniker),
                  new ClassicExecutor())
        {
        }

        public override bool IsSupported(Benchmark benchmark, ILogger logger)
        {
            if (benchmark.Job.Platform == Platform.X86)
            {
                logger.Write(LogKind.Error, $"Currently dotnet cli toolchain supports only X64 compilation, benchmark {benchmark.ShortInfo} will not be executed");
                return false;
            }
            if (benchmark.Job.Jit == Jit.LegacyJit)
            {
                logger.Write(LogKind.Error, $"Currently dotnet cli toolchain supports only RyuJit, benchmark {benchmark.ShortInfo} will not be executed");
                return false;
            }

            return true;
        }
    }
}