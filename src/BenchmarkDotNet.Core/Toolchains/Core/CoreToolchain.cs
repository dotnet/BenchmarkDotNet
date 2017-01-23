using System;
using System.Reflection;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Core
{
    public class CoreToolchain : Toolchain
    {
        public static readonly IToolchain NetCoreApp11 = From(NetCoreAppSettings.NetCoreApp11);
        public static readonly IToolchain NetCoreApp12 = From(NetCoreAppSettings.NetCoreApp12);
        public static readonly IToolchain NetCoreApp20 = From(NetCoreAppSettings.NetCoreApp20);

        private static IToolchain Default => NetCoreApp11;

        public static readonly Lazy<IToolchain> Current = new Lazy<IToolchain>(GetCurrentVersion);

        private CoreToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor) 
            : base(name, generator, builder, executor)
        {
        }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings) 
            => new CoreToolchain(
                "Core", 
                new DotNetCliGenerator(
                    settings.TargetFrameworkMoniker,
                    GetExtraDependencies(settings),
                    PlatformProvider,
                    settings.Imports,
                    GetRuntime()), 
                new DotNetCliBuilder(settings.TargetFrameworkMoniker), 
                new Executor());

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if(!base.IsSupported(benchmark, logger, resolver))
            {
                return false;
            }

            if (!HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (benchmark.Job.ResolveValue(EnvMode.PlatformCharacteristic, resolver) == Platform.X86)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only X64 compilation, benchmark '{benchmark.DisplayInfo}' will not be executed");
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
            var currentRuntime = RuntimeInformation.GetDotNetCliRuntimeIdentifier();
            if (!string.IsNullOrEmpty(currentRuntime))
            {
                return $"\"runtimes\": {{ \"{currentRuntime}\": {{ }} }},";
            }

            return string.Empty;
        }

        private static IToolchain GetCurrentVersion()
        {
#if CLASSIC
            return Default;
#else
            try
            {
                // it's an experimental way to determine the .NET Core Runtime version
                // based on dev packages available at https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App
                var assembly = Assembly.Load(new AssemblyName("System.Runtime"));
                if (assembly.FullName.Contains("Version=4.1.1"))
                    return NetCoreApp11;

                // the problem is that both netcoreapp1.2 and netcoreapp2.0 have 
                // "System.Runtime Version=1.2.0.0". 
                // 2.0 was officialy announced name, so let's bet on it (1.2 was probably an internal dev thing)
                if (assembly.FullName.Contains("Version=4.2"))
                    return NetCoreApp20;
            }
            catch
            {
                return Default;
            }

            return Default;
#endif
        }
    }
}