using System;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    [PublicAPI]
    public class CsProjCoreToolchain : Toolchain
    {
        [PublicAPI] public static readonly IToolchain NetCoreApp20 = From(NetCoreAppSettings.NetCoreApp20);
        [PublicAPI] public static readonly IToolchain NetCoreApp21 = From(NetCoreAppSettings.NetCoreApp21);
        [PublicAPI] public static readonly IToolchain NetCoreApp22 = From(NetCoreAppSettings.NetCoreApp22);
        [PublicAPI] public static readonly IToolchain NetCoreApp30 = From(NetCoreAppSettings.NetCoreApp30);

        [PublicAPI] public static readonly Lazy<IToolchain> Current = new Lazy<IToolchain>(() => From(NetCoreAppSettings.GetCurrentVersion()));

        private CsProjCoreToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath) 
            : base(name, generator, builder, executor)
        {
            CustomDotNetCliPath = customDotNetCliPath;
        }

        internal string CustomDotNetCliPath { get; }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings)
            => new CsProjCoreToolchain(settings.Name,
                new CsProjGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.RuntimeFrameworkVersion), 
                new DotNetCliBuilder(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.Timeout), 
                new DotNetCliExecutor(settings.CustomDotNetCliPath),
                settings.CustomDotNetCliPath);

        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmarkCase, logger, resolver))
                return false;

            if (InvalidCliPath(CustomDotNetCliPath, benchmarkCase, logger))
                return false;

            if (benchmarkCase.Job.HasValue(EnvironmentMode.JitCharacteristic) && benchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver) == Jit.LegacyJit)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only RyuJit, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }
            if (benchmarkCase.Job.ResolveValue(GcMode.CpuGroupsCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently project.json does not support CpuGroups (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }
            if (benchmarkCase.Job.ResolveValue(GcMode.AllowVeryLargeObjectsCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently project.json does not support gcAllowVeryLargeObjects (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }
    }
}