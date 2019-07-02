using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.Roslyn;
using System;

namespace BenchmarkDotNet.Extensions
{
    internal static class TargetFrameworkMonikerExtensions
    {
        internal static IToolchain GetToolchain(this TargetFrameworkMoniker targetFrameworkMoniker)
        {
            switch (targetFrameworkMoniker)
            {
                case TargetFrameworkMoniker.Net461:
                    return CsProjClassicNetToolchain.Net461;
                case TargetFrameworkMoniker.Net462:
                    return CsProjClassicNetToolchain.Net462;
                case TargetFrameworkMoniker.Net47:
                    return CsProjClassicNetToolchain.Net47;
                case TargetFrameworkMoniker.Net471:
                    return CsProjClassicNetToolchain.Net471;
                case TargetFrameworkMoniker.Net472:
                    return CsProjClassicNetToolchain.Net472;
                case TargetFrameworkMoniker.Net48:
                    return CsProjClassicNetToolchain.Net48;
                case TargetFrameworkMoniker.Netcoreapp20:
                    return CsProjCoreToolchain.NetCoreApp20;
                case TargetFrameworkMoniker.Netcoreapp21:
                    return CsProjCoreToolchain.NetCoreApp21;
                case TargetFrameworkMoniker.Netcoreapp22:
                    return CsProjCoreToolchain.NetCoreApp22;
                case TargetFrameworkMoniker.Netcoreapp30:
                    return CsProjCoreToolchain.NetCoreApp30;
                case TargetFrameworkMoniker.Mono:
                    return RoslynToolchain.Instance;
                case TargetFrameworkMoniker.CoreRt:
                    return CoreRtToolchain.LatestBuild;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetFrameworkMoniker), targetFrameworkMoniker, "Target Framework Moniker not supported");
            }
        }

        internal static Runtime GetRuntime(this TargetFrameworkMoniker targetFrameworkMoniker)
        {
            switch (targetFrameworkMoniker)
            {
                case TargetFrameworkMoniker.Net461:
                case TargetFrameworkMoniker.Net462:
                case TargetFrameworkMoniker.Net47:
                case TargetFrameworkMoniker.Net471:
                case TargetFrameworkMoniker.Net472:
                case TargetFrameworkMoniker.Net48:
                    return Runtime.Clr;
                case TargetFrameworkMoniker.Netcoreapp20:
                case TargetFrameworkMoniker.Netcoreapp21:
                case TargetFrameworkMoniker.Netcoreapp22:
                case TargetFrameworkMoniker.Netcoreapp30:
                    return Runtime.Core;
                case TargetFrameworkMoniker.Mono:
                    return Runtime.Mono;
                case TargetFrameworkMoniker.CoreRt:
                    return Runtime.CoreRT;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetFrameworkMoniker), targetFrameworkMoniker, "Target Framework Moniker not supported");
            }
        }
    }
}
