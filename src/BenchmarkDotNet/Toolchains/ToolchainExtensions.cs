using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.Mono;
using BenchmarkDotNet.Toolchains.Roslyn;

namespace BenchmarkDotNet.Toolchains
{
    internal static class ToolchainExtensions
    {
        internal static IToolchain GetToolchain(this BenchmarkCase benchmarkCase) => GetToolchain(benchmarkCase.Job, benchmarkCase.Descriptor);

        internal static IToolchain GetToolchain(this Job job) => GetToolchain(job, null);

        private static IToolchain GetToolchain(Job job, Descriptor descriptor)
            => job.HasValue(InfrastructureMode.ToolchainCharacteristic)
                ? job.Infrastructure.Toolchain
                : GetToolchain(
                    job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, EnvironmentResolver.Instance),
                    descriptor,
                    job.HasValue(InfrastructureMode.NuGetReferencesCharacteristic) || job.HasValue(InfrastructureMode.BuildConfigurationCharacteristic));

        internal static IToolchain GetToolchain(this Runtime runtime, Descriptor descriptor = null, bool preferMsBuildToolchains = false)
        {
            switch (runtime)
            {
                case ClrRuntime clrRuntime:
                    if (RuntimeInformation.IsNetCore || preferMsBuildToolchains)
                        return clrRuntime.TargetFrameworkMoniker != TargetFrameworkMoniker.NotRecognized
                            ? GetToolchain(clrRuntime.TargetFrameworkMoniker)
                            : CsProjClassicNetToolchain.From(clrRuntime.MsBuildMoniker);

                    return RoslynToolchain.Instance;

                case MonoRuntime mono:
                    if(!string.IsNullOrEmpty(mono.AotArgs))
                        return MonoAotToolchain.Instance;

                    return RoslynToolchain.Instance;

                case CoreRuntime coreRuntime:
                    if (descriptor != null && descriptor.Type.Assembly.IsLinqPad())
                        return InProcessEmitToolchain.Instance;
                    if (coreRuntime.TargetFrameworkMoniker != TargetFrameworkMoniker.NotRecognized)
                        return GetToolchain(coreRuntime.TargetFrameworkMoniker);
                    
                    return CsProjCoreToolchain.From(new DotNetCli.NetCoreAppSettings(coreRuntime.MsBuildMoniker, null, coreRuntime.Name));

                case CoreRtRuntime coreRtRuntime:
                    return coreRtRuntime.TargetFrameworkMoniker != TargetFrameworkMoniker.NotRecognized
                            ? GetToolchain(coreRtRuntime.TargetFrameworkMoniker)
                            : CoreRtToolchain.CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker(coreRtRuntime.MsBuildMoniker).ToToolchain();

                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }

        private static IToolchain GetToolchain(TargetFrameworkMoniker targetFrameworkMoniker)
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
                case TargetFrameworkMoniker.CoreRt20:
                    return CoreRtToolchain.Core20;
                case TargetFrameworkMoniker.CoreRt21:
                    return CoreRtToolchain.Core21;
                case TargetFrameworkMoniker.CoreRt22:
                    return CoreRtToolchain.Core22;
                case TargetFrameworkMoniker.CoreRt30:
                    return CoreRtToolchain.Core30;
                case TargetFrameworkMoniker.CoreRt31:
                    return CoreRtToolchain.Core31;
                case TargetFrameworkMoniker.CoreRt50:
                    return CoreRtToolchain.Core50;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetFrameworkMoniker), targetFrameworkMoniker, "Target Framework Moniker not supported");
            }
        }
    }
}