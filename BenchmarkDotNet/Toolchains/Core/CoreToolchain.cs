using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Classic;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Core
{
    public class CoreToolchain : Toolchain
    {
        private const string TargetFrameworkMoniker = "netcoreapp1.0";

        public static readonly IToolchain Instance = new CoreToolchain();

        private CoreToolchain()
            : base("Core",
                  new DotNetCliGenerator(
                      TargetFrameworkMoniker, 
                      GetExtraDependencies(), 
                      platformProvider: _ => "x64", // dotnet cli supports only x64 compilation now
                      imports: GetImports(),
                      runtime: GetRuntime()), 
                  new DotNetCliBuilder(TargetFrameworkMoniker),
                  new ClassicExecutor())
        {
        }

        public override bool IsSupported(Benchmark benchmark, ILogger logger)
        {
            if(!base.IsSupported(benchmark, logger))
            {
                return false;
            }

            if (!HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
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
            if (benchmark.Job.GarbageCollection.CpuGroups)
            {
                logger.WriteLineError($"Currently project.json does not support CpuGroups (app.config does), benchmark {benchmark.ShortInfo} will not be executed");
                return false;
            }
            if (benchmark.Job.GarbageCollection.AllowVeryLargeObjects)
            {
                logger.WriteLineError($"Currently project.json does not support gcAllowVeryLargeObjects (app.config does), benchmark {benchmark.ShortInfo} will not be executed");
                return false;
            }

            return true;
        }

        private static string GetExtraDependencies()
        {
            // do not set the type to platform in order to produce exe
            // https://github.com/dotnet/core/issues/77#issuecomment-219692312
            return "\"dependencies\": { \"Microsoft.NETCore.App\": { \"version\": \"1.0.0\" } },";
        }

        private static string GetImports()
        {
            return "[ \"dnxcore50\", \"portable-net45+win8\", \"dotnet5.6\", \"netcore50\" ]";
        }

        private static string GetRuntime()
        {
            var currentRuntime = RuntimeInformation.GetDotNetCliRuntimeIdentifier();
            if (!string.IsNullOrEmpty(currentRuntime))
            {
                return $"\"runtimes\": {{ \"{currentRuntime}\": {{ }} }},";
            }

            return string.Empty;
        }
    }
}