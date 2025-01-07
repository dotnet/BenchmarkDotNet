using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    [PublicAPI]
    public class CsProjCoreToolchain : Toolchain, IEquatable<CsProjCoreToolchain>
    {
        [Obsolete("This runtime is no longer supported. Use a newer runtime or use BenchmarkDotNet v0.14.X or older.", true)]
        [PublicAPI] public static readonly IToolchain NetCoreApp20 = From(NetCoreAppSettings.NetCoreApp20);
        [Obsolete("This runtime is no longer supported. Use a newer runtime or use BenchmarkDotNet v0.14.X or older.", true)]
        [PublicAPI] public static readonly IToolchain NetCoreApp21 = From(NetCoreAppSettings.NetCoreApp21);
        [Obsolete("This runtime is no longer supported. Use a newer runtime or use BenchmarkDotNet v0.14.X or older.", true)]
        [PublicAPI] public static readonly IToolchain NetCoreApp22 = From(NetCoreAppSettings.NetCoreApp22);
        [Obsolete("This runtime is no longer supported. Use a newer runtime or use BenchmarkDotNet v0.14.X or older.", true)]
        [PublicAPI] public static readonly IToolchain NetCoreApp30 = From(NetCoreAppSettings.NetCoreApp30);
        [PublicAPI] public static readonly IToolchain NetCoreApp31 = From(NetCoreAppSettings.NetCoreApp31);
        [PublicAPI] public static readonly IToolchain NetCoreApp50 = From(NetCoreAppSettings.NetCoreApp50);
        [PublicAPI] public static readonly IToolchain NetCoreApp60 = From(NetCoreAppSettings.NetCoreApp60);
        [PublicAPI] public static readonly IToolchain NetCoreApp70 = From(NetCoreAppSettings.NetCoreApp70);
        [PublicAPI] public static readonly IToolchain NetCoreApp80 = From(NetCoreAppSettings.NetCoreApp80);
        [PublicAPI] public static readonly IToolchain NetCoreApp90 = From(NetCoreAppSettings.NetCoreApp90);
        [PublicAPI] public static readonly IToolchain NetCoreApp10_0 = From(NetCoreAppSettings.NetCoreApp10_0);

        internal CsProjCoreToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor)
        {
            CustomDotNetCliPath = customDotNetCliPath;
        }

        internal string CustomDotNetCliPath { get; }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings)
            => new CsProjCoreToolchain(settings.Name,
                new CsProjGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.RuntimeFrameworkVersion),
                new DotNetCliBuilder(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath),
                new DotNetCliExecutor(settings.CustomDotNetCliPath),
                settings.CustomDotNetCliPath);

        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var validationError in base.Validate(benchmarkCase, resolver))
            {
                yield return validationError;
            }

            if (benchmarkCase.Job.HasValue(EnvironmentMode.JitCharacteristic) && benchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver) == Jit.LegacyJit)
            {
                yield return new ValidationError(true,
                    $"Currently dotnet cli toolchain supports only RyuJit, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);
            }
            if (benchmarkCase.Job.ResolveValue(GcMode.CpuGroupsCharacteristic, resolver))
            {
                yield return new ValidationError(true,
                    $"Currently project.json does not support CpuGroups (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);
            }
            if (benchmarkCase.Job.ResolveValue(GcMode.AllowVeryLargeObjectsCharacteristic, resolver))
            {
                yield return new ValidationError(true,
                    $"Currently project.json does not support gcAllowVeryLargeObjects (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);
            }

            var benchmarkAssembly = benchmarkCase.Descriptor.Type.Assembly;
            if (benchmarkAssembly.IsLinqPad())
            {
                yield return new ValidationError(true,
                    $"Currently CsProjCoreToolchain does not support LINQPad 6+. Please use {nameof(InProcessEmitToolchain)} instead.",
                    benchmarkCase);
            }

            foreach (var validationError in DotNetSdkValidator.ValidateCoreSdks(CustomDotNetCliPath, benchmarkCase))
            {
                yield return validationError;
            }
        }

        public override bool Equals(object obj) => obj is CsProjCoreToolchain typed && Equals(typed);

        public bool Equals(CsProjCoreToolchain other) => Generator.Equals(other.Generator);

        public override int GetHashCode() => Generator.GetHashCode();
    }
}