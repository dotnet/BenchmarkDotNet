using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Classic;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Core
{
    public class CoreToolchain : Toolchain
    {
        private const string TargetFrameworkMoniker = "dnxcore50"; // todo: when dnx gets replaced in VS with dotnet cli replace this with the new name

        public static readonly IToolchain Instance = new CoreToolchain();

        private CoreToolchain()
            : base("Core",
                  new DotNetCliGenerator(
                      _ => TargetFrameworkMoniker, 
                      extraDependencies: "\"dependencies\": { \"NETStandard.Library\": \"1.0.0-rc2-23811\" }", // required by dotnet cli
                      platformProvider: _ => "x64"), // dotnet cli supports only x64 compilation now
                  new DotNetCliBuilder(_ => TargetFrameworkMoniker),
                  new ClassicExecutor())
        {
        }

        public override bool IsSupported(Benchmark benchmark, ILogger logger)
        {
            if (!EnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark {benchmark.ShortInfo} will not be executed");
                return false;
            }

            if (benchmark.Job.Platform == Platform.X86)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only X64 compilation, benchmark {benchmark.ShortInfo} will not be executed");
                return false;
            }
            if (benchmark.Job.Jit == Jit.LegacyJit)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only RyuJit, benchmark {benchmark.ShortInfo} will not be executed");
                return false;
            }

            return true;
        }
    }
}