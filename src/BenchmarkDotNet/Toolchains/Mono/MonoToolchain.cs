using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Toolchains.Mono
{
    [PublicAPI]
    public class MonoToolchain : CsProjCoreToolchain, IEquatable<MonoToolchain>
    {
        [PublicAPI] public static readonly IToolchain Mono60 = From(new NetCoreAppSettings("net6.0", null, "mono60"));
        [PublicAPI] public static readonly IToolchain Mono70 = From(new NetCoreAppSettings("net7.0", null, "mono70"));
        [PublicAPI] public static readonly IToolchain Mono80 = From(new NetCoreAppSettings("net8.0", null, "mono80"));
        [PublicAPI] public static readonly IToolchain Mono90 = From(new NetCoreAppSettings("net9.0", null, "mono90"));

        private MonoToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor, customDotNetCliPath)
        {
        }

        [PublicAPI]
        public static new IToolchain From(NetCoreAppSettings settings)
        {
            return new MonoToolchain(settings.Name,
                        new MonoGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.RuntimeFrameworkVersion),
                        new MonoPublisher(settings.CustomDotNetCliPath),
                        new DotNetCliExecutor(settings.CustomDotNetCliPath),
                        settings.CustomDotNetCliPath);
        }

        public override bool Equals(object obj) => obj is MonoToolchain typed && Equals(typed);

        public bool Equals(MonoToolchain other) => Generator.Equals(other.Generator);

        public override int GetHashCode() => Generator.GetHashCode();
    }
}
