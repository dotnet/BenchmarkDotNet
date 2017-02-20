using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.ProjectJson
{
    [PublicAPI]
    public class ProjectJsonNet46Toolchain : Toolchain
    {
        [PublicAPI] public static readonly IToolchain Instance = new ProjectJsonNet46Toolchain();

        // In case somebody calls ClassicToolchain from .NET Core process 
        // we will build the project as 4.6 because it's the most safe way to do it:
        // * everybody that uses .NET Core must have VS 2015 installed and 4.6 is part of the installation
        // * from 4.6 you can target < 4.6
        private const string TargetFrameworkMoniker = "net46";

        [PublicAPI]
        public ProjectJsonNet46Toolchain() : base(
            "Classic",
            new ProjectJsonGenerator(
                TargetFrameworkMoniker,
                extraDependencies: "\"frameworkAssemblies\": { \"System.Runtime\": \"4.0.0.0\" },",
                platformProvider: platform => platform.ToConfig(),
                imports: "\"portable-net45+win8\""),
            new DotNetCliBuilder(TargetFrameworkMoniker),
            new Executor())
        {
        }

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmark, logger, resolver))
            {
                return false;
            }

            if (!HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (benchmark.Job.ResolveValue(EnvMode.JitCharacteristic, resolver) == Jit.LegacyJit)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only RyuJit, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }
            if (benchmark.Job.ResolveValue(GcMode.CpuGroupsCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently project.json does not support CpuGroups (app.config does), benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }
            if (benchmark.Job.ResolveValue(GcMode.AllowVeryLargeObjectsCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently project.json does not support gcAllowVeryLargeObjects (app.config does), benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }

    }
}