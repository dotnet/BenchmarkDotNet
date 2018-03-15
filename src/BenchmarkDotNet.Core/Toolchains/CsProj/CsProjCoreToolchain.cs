﻿using System;
using System.IO;
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
        [PublicAPI] public static readonly IToolchain NetCoreApp21 = From(NetCoreAppSettings.NetCoreApp21);

        [PublicAPI] public static readonly Lazy<IToolchain> Current = new Lazy<IToolchain>(() => From(NetCoreAppSettings.GetCurrentVersion()));

        private CsProjCoreToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath) 
            : base(name, generator, builder, executor)
        {
            CustomDotNetCliPath = customDotNetCliPath;
        }

        private string CustomDotNetCliPath { get; }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings)
            => new CsProjCoreToolchain(settings.Name,
                new CsProjGenerator(settings.TargetFrameworkMoniker, PlatformProvider, settings.RuntimeFrameworkVersion), 
                new CsProjBuilder(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath), 
                new DotNetCliExecutor(settings.CustomDotNetCliPath),
                settings.CustomDotNetCliPath);

        private static string PlatformProvider(Platform platform) => platform.ToConfig();

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmark, logger, resolver))
            {
                return false;
            }

            if (string.IsNullOrEmpty(CustomDotNetCliPath) && !HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (!string.IsNullOrEmpty(CustomDotNetCliPath) && !File.Exists(CustomDotNetCliPath))
            {
                logger.WriteLineError($"Povided custom dotnet cli path does not exist, benchmark '{benchmark.DisplayInfo}' will not be executed");
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