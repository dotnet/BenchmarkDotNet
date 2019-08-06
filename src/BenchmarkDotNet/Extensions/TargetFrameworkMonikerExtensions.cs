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
                case TargetFrameworkMoniker.NetCoreApp20:
                    return CsProjCoreToolchain.NetCoreApp20;
                case TargetFrameworkMoniker.NetCoreApp21:
                    return CsProjCoreToolchain.NetCoreApp21;
                case TargetFrameworkMoniker.NetCoreApp22:
                    return CsProjCoreToolchain.NetCoreApp22;
                case TargetFrameworkMoniker.NetCoreApp30:
                    return CsProjCoreToolchain.NetCoreApp30;
                case TargetFrameworkMoniker.NetCoreApp31:
                    return CsProjCoreToolchain.NetCoreApp31;
                case TargetFrameworkMoniker.NetCoreApp50:
                    return CsProjCoreToolchain.NetCoreApp50;
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
                case TargetFrameworkMoniker.NetCoreApp20:
                case TargetFrameworkMoniker.NetCoreApp21:
                case TargetFrameworkMoniker.NetCoreApp22:
                case TargetFrameworkMoniker.NetCoreApp30:
                case TargetFrameworkMoniker.NetCoreApp31:
                case TargetFrameworkMoniker.NetCoreApp50:
                    return Runtime.Core;
                case TargetFrameworkMoniker.Mono:
                    return Runtime.Mono;
                case TargetFrameworkMoniker.CoreRt:
                    return Runtime.CoreRT;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetFrameworkMoniker), targetFrameworkMoniker, "Target Framework Moniker not supported");
            }
        }

        internal static string ToMsBuildName(this TargetFrameworkMoniker targetFrameworkMoniker)
        {
            switch (targetFrameworkMoniker)
            {
                case TargetFrameworkMoniker.Net461:
                    return "net461";
                case TargetFrameworkMoniker.Net462:
                    return "net462";
                case TargetFrameworkMoniker.Net47:
                    return "net47";
                case TargetFrameworkMoniker.Net471:
                    return "net471";
                case TargetFrameworkMoniker.Net472:
                    return "net472";
                case TargetFrameworkMoniker.Net48:
                    return "net48";
                case TargetFrameworkMoniker.NetCoreApp20:
                    return "netcoreapp2.0";
                case TargetFrameworkMoniker.NetCoreApp21:
                    return "netcoreapp2.1";
                case TargetFrameworkMoniker.NetCoreApp22:
                    return "netcoreapp2.2";
                case TargetFrameworkMoniker.NetCoreApp30:
                    return "netcoreapp3.0";
                case TargetFrameworkMoniker.NetCoreApp31:
                    return "netcoreapp3.1";
                case TargetFrameworkMoniker.NetCoreApp50:
                    return "netcoreapp5.0";
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetFrameworkMoniker), targetFrameworkMoniker, "Target Framework Moniker not supported");
            }
        }
    }
}
