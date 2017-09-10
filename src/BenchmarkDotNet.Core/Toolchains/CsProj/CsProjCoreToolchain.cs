using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
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
        [PublicAPI] public static readonly IToolchain NetCoreApp11 = From(NetCoreAppSettings.NetCoreApp11);
        [PublicAPI] public static readonly IToolchain NetCoreApp12 = From(NetCoreAppSettings.NetCoreApp12);
        [PublicAPI] public static readonly IToolchain NetCoreApp20 = From(NetCoreAppSettings.NetCoreApp20);

        [PublicAPI] public static readonly Lazy<IToolchain> Current = new Lazy<IToolchain>(() => From(NetCoreAppSettings.GetCurrentVersion()));

        private CsProjCoreToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor) 
            : base(name, generator, builder, executor)
        {
        }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings)
            => new CsProjCoreToolchain(settings.Name,
                new CsProjGenerator(settings.TargetFrameworkMoniker, PlatformProvider, settings.RuntimeFrameworkVersion), 
                new CsProjBuilder(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath), 
                new DotNetCliExecutor(settings.CustomDotNetCliPath));

        private static string PlatformProvider(Platform platform) => platform.ToConfig();

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

#if NETCOREAPP1_1
            if (benchmark.Job.HasValue(InfrastructureMode.EnvironmentVariablesCharacteristic))
            {
                logger.WriteLineError($"ProcessStartInfo.EnvironmentVariables is avaialable for .NET Core 2.0, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }
#endif

            return true;
        }
    }
}