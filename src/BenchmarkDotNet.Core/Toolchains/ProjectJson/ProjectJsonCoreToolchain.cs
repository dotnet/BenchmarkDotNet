using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.ProjectJson
{
    [PublicAPI]
    public class ProjectJsonCoreToolchain : Toolchain
    {
        [PublicAPI] public static IToolchain GetNetCoreApp11(RuntimeInformation runtimeInformation) => From(NetCoreAppSettings.NetCoreApp11, runtimeInformation);
        [PublicAPI] public static IToolchain GetNetCoreApp20(RuntimeInformation runtimeInformation) => From(NetCoreAppSettings.NetCoreApp20, runtimeInformation);

        [PublicAPI] public static IToolchain GetCurrent(RuntimeInformation runtimeInformation) => From(NetCoreAppSettings.GetCurrentVersion(), runtimeInformation);

        private ProjectJsonCoreToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, RuntimeInformation runtimeInformation) 
            : base(name, generator, builder, executor, runtimeInformation)
        {
        }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings, RuntimeInformation runtimeInformation) 
            => new ProjectJsonCoreToolchain(
                "Core", 
                new ProjectJsonGenerator(
                    settings.TargetFrameworkMoniker,
                    GetExtraDependencies(settings),
                    PlatformProvider,
                    settings.Imports,
                    GetRuntime()), 
                new ProjectJsonBuilder(settings.TargetFrameworkMoniker), 
                new Executor(),
                runtimeInformation);

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if(!base.IsSupported(benchmark, logger, resolver))
            {
                return false;
            }

            if (RuntimeInformation.IsMono)
            {
                logger.WriteLineError($"BenchmarkDotNet does not support running .NET Core benchmarks when host process is Mono, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (!HostEnvironmentInfo.GetCurrent(RuntimeInformation).IsDotNetCliInstalled())
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

        // dotnet cli supports only x64 compilation now
        private static string PlatformProvider(Platform platform) => "x64";

        private static string GetExtraDependencies(NetCoreAppSettings settings)
        {
            // do not set the type to platform in order to produce exe
            // https://github.com/dotnet/core/issues/77#issuecomment-219692312
            return $"\"dependencies\": {{ \"Microsoft.NETCore.App\": {{ \"version\": \"{settings.MicrosoftNETCoreAppVersion}\" }} }},";
        }

        private static string GetRuntime()
        {
            var currentRuntime = Microsoft.DotNet.InternalAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();
            if (!string.IsNullOrEmpty(currentRuntime))
            {
                return $"\"runtimes\": {{ \"{currentRuntime}\": {{ }} }},";
            }

            return string.Empty;
        }
    }
}
