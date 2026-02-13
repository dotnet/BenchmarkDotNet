using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchains.R2R
{
    [PublicAPI]
    public class R2RToolchain : CsProjCoreToolchain, IEquatable<R2RToolchain>
    {
        [PublicAPI] public static readonly IToolchain R2R80 = From(new NetCoreAppSettings("net8.0", null, "R2R 8.0"));
        [PublicAPI] public static readonly IToolchain R2R90 = From(new NetCoreAppSettings("net9.0", null, "R2R 9.0"));
        [PublicAPI] public static readonly IToolchain R2R10_0 = From(new NetCoreAppSettings("net10.0", null, "R2R 10.0"));
        [PublicAPI] public static readonly IToolchain R2R11_0 = From(new NetCoreAppSettings("net11.0", null, "R2R 11.0"));

        private readonly string _customDotNetCliPath;
        private R2RToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor, customDotNetCliPath)
        {
            _customDotNetCliPath = customDotNetCliPath;
        }

        [PublicAPI]
        public static new IToolchain From(NetCoreAppSettings settings)
            => new R2RToolchain(settings.Name,
                new R2RGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.CustomRuntimePack, settings.AOTCompilerPath),
                new DotNetCliPublisher(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath),
                new Executor(),
                settings.CustomDotNetCliPath);

        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var validationError in DotNetSdkValidator.ValidateCoreSdks(_customDotNetCliPath, benchmarkCase))
            {
                yield return validationError;
            }
        }

        public override bool Equals(object? obj) => obj is R2RToolchain typed && Equals(typed);

        public bool Equals(R2RToolchain? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is null)
                return false;

            return Generator.Equals(other.Generator);
        }

        public override int GetHashCode() => Generator.GetHashCode();
    }
}
