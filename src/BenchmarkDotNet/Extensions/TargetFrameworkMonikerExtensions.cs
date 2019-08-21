using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Extensions
{
    internal static class TargetFrameworkMonikerExtensions
    {
        internal static Runtime GetRuntime(this TargetFrameworkMoniker targetFrameworkMoniker)
        {
            switch (targetFrameworkMoniker)
            {
                case TargetFrameworkMoniker.Net461:
                    return ClrRuntime.Net461;
                case TargetFrameworkMoniker.Net462:
                    return ClrRuntime.Net462;
                case TargetFrameworkMoniker.Net47:
                    return ClrRuntime.Net47;
                case TargetFrameworkMoniker.Net471:
                    return ClrRuntime.Net471;
                case TargetFrameworkMoniker.Net472:
                    return ClrRuntime.Net472;
                case TargetFrameworkMoniker.Net48:
                    return ClrRuntime.Net48;
                case TargetFrameworkMoniker.NetCoreApp20:
                    return CoreRuntime.Core20;
                case TargetFrameworkMoniker.NetCoreApp21:
                    return CoreRuntime.Core21;
                case TargetFrameworkMoniker.NetCoreApp22:
                    return CoreRuntime.Core22;
                case TargetFrameworkMoniker.NetCoreApp30:
                    return CoreRuntime.Core30;
                case TargetFrameworkMoniker.NetCoreApp31:
                    return CoreRuntime.Core31;
                case TargetFrameworkMoniker.NetCoreApp50:
                    return CoreRuntime.Core50;
                case TargetFrameworkMoniker.Mono:
                    return MonoRuntime.Default;
                case TargetFrameworkMoniker.CoreRt20:
                    return CoreRtRuntime.CoreRt20;
                case TargetFrameworkMoniker.CoreRt21:
                    return CoreRtRuntime.CoreRt21;
                case TargetFrameworkMoniker.CoreRt22:
                    return CoreRtRuntime.CoreRt22;
                case TargetFrameworkMoniker.CoreRt30:
                    return CoreRtRuntime.CoreRt30;
                case TargetFrameworkMoniker.CoreRt31:
                    return CoreRtRuntime.CoreRt31;
                case TargetFrameworkMoniker.CoreRt50:
                    return CoreRtRuntime.CoreRt50;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetFrameworkMoniker), targetFrameworkMoniker, "Target Framework Moniker not supported");
            }
        }
    }
}
