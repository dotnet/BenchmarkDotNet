using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    [PublicAPI]
    public class CsProjCoreToolchain : Toolchain
    {
        [PublicAPI] public static IToolchain GetNetCoreApp11(RuntimeInformation runtimeInformation) => From(NetCoreAppSettings.NetCoreApp11, runtimeInformation);
        [PublicAPI] public static IToolchain GetNetCoreApp20(RuntimeInformation runtimeInformation) => From(NetCoreAppSettings.NetCoreApp20, runtimeInformation);

        [PublicAPI] public static IToolchain GetCurrent(RuntimeInformation runtimeInformation) => From(NetCoreAppSettings.GetCurrentVersion(), runtimeInformation);

        private CsProjCoreToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, RuntimeInformation runtimeInformation) 
            : base(name, generator, builder, executor, runtimeInformation)
        {
        }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings, RuntimeInformation runtimeInformation)
            => new CsProjCoreToolchain("CoreCsProj",
                new CsProjGenerator(settings.TargetFrameworkMoniker, PlatformProvider), 
                new CsProjBuilder(settings.TargetFrameworkMoniker), 
                new DotNetCliExecutor(),
                runtimeInformation);

        // dotnet cli supports only x64 compilation now
        private static string PlatformProvider(Platform platform) => "x64";

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmark, logger, resolver))
            {
                return false;
            }

            if (RuntimeInformation.IsMono())
            {
                logger.WriteLineError($"BenchmarkDotNet does not support running .NET Core benchmarks when host process is Mono, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (!HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (benchmark.Job.HasValue(EnvMode.PlatformCharacteristic) && benchmark.Job.ResolveValue(EnvMode.PlatformCharacteristic, resolver) == Platform.X86)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only X64 compilation, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }
            if (benchmark.Job.HasValue(EnvMode.JitCharacteristic) && benchmark.Job.ResolveValue(EnvMode.JitCharacteristic, resolver) == Jit.LegacyJit)
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
