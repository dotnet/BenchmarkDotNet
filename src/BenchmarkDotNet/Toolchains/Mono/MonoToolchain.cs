using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;
using System;

#nullable enable

namespace BenchmarkDotNet.Toolchains.Mono
{
    [PublicAPI]
    public class MonoToolchain : CsProjCoreToolchain, IEquatable<MonoToolchain>
    {
        [PublicAPI] public static readonly IToolchain Mono60 = From(new NetCoreAppSettings("net6.0", "mono60"));
        [PublicAPI] public static readonly IToolchain Mono70 = From(new NetCoreAppSettings("net7.0", "mono70"));
        [PublicAPI] public static readonly IToolchain Mono80 = From(new NetCoreAppSettings("net8.0", "mono80"));

        private MonoToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor, customDotNetCliPath)
        {
        }

        [PublicAPI]
        public static new IToolchain From(NetCoreAppSettings settings)
            => new MonoToolchain(settings.Name,
                new MonoGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.RuntimeFrameworkVersion),
                new MonoPublisher(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath),
                new DotNetCliExecutor(settings.CustomDotNetCliPath),
                settings.CustomDotNetCliPath);

        public override bool Equals(object? obj) => obj is MonoToolchain typed && Equals(typed);

        public bool Equals(MonoToolchain? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Generator.Equals(other.Generator);
        }

        public override int GetHashCode() => Generator.GetHashCode();
    }
}
