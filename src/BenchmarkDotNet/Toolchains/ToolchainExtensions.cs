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
            => job.Infrastructure.TryGetToolchain(out var toolchain)
                ? toolchain
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
                        return clrRuntime.RuntimeMoniker != RuntimeMoniker.NotRecognized
                            ? GetToolchain(clrRuntime.RuntimeMoniker)
                            : CsProjClassicNetToolchain.From(clrRuntime.MsBuildMoniker);

                    return RoslynToolchain.Instance;

                case MonoRuntime mono:
                    if(!string.IsNullOrEmpty(mono.AotArgs))
                        return MonoAotToolchain.Instance;

                    return RoslynToolchain.Instance;

                case CoreRuntime coreRuntime:
                    if (descriptor != null && descriptor.Type.Assembly.IsLinqPad())
                        return InProcessEmitToolchain.Instance;
                    if (coreRuntime.RuntimeMoniker != RuntimeMoniker.NotRecognized)
                        return GetToolchain(coreRuntime.RuntimeMoniker);

                    return CsProjCoreToolchain.From(new DotNetCli.NetCoreAppSettings(coreRuntime.MsBuildMoniker, null, coreRuntime.Name));

                case CoreRtRuntime coreRtRuntime:
                    return coreRtRuntime.RuntimeMoniker != RuntimeMoniker.NotRecognized
                            ? GetToolchain(coreRtRuntime.RuntimeMoniker)
                            : CoreRtToolchain.CreateBuilder().UseCoreRtNuGet().TargetFrameworkMoniker(coreRtRuntime.MsBuildMoniker).ToToolchain();

                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }

        private static IToolchain GetToolchain(RuntimeMoniker runtimeMoniker)
        {
            switch (runtimeMoniker)
            {
                case RuntimeMoniker.Net461:
                    return CsProjClassicNetToolchain.Net461;
                case RuntimeMoniker.Net462:
                    return CsProjClassicNetToolchain.Net462;
                case RuntimeMoniker.Net47:
                    return CsProjClassicNetToolchain.Net47;
                case RuntimeMoniker.Net471:
                    return CsProjClassicNetToolchain.Net471;
                case RuntimeMoniker.Net472:
                    return CsProjClassicNetToolchain.Net472;
                case RuntimeMoniker.Net48:
                    return CsProjClassicNetToolchain.Net48;
                case RuntimeMoniker.NetCoreApp20:
                    return CsProjCoreToolchain.NetCoreApp20;
                case RuntimeMoniker.NetCoreApp21:
                    return CsProjCoreToolchain.NetCoreApp21;
                case RuntimeMoniker.NetCoreApp22:
                    return CsProjCoreToolchain.NetCoreApp22;
                case RuntimeMoniker.NetCoreApp30:
                    return CsProjCoreToolchain.NetCoreApp30;
                case RuntimeMoniker.NetCoreApp31:
                    return CsProjCoreToolchain.NetCoreApp31;
                case RuntimeMoniker.NetCoreApp50:
                    return CsProjCoreToolchain.NetCoreApp50;
                case RuntimeMoniker.CoreRt20:
                    return CoreRtToolchain.Core20;
                case RuntimeMoniker.CoreRt21:
                    return CoreRtToolchain.Core21;
                case RuntimeMoniker.CoreRt22:
                    return CoreRtToolchain.Core22;
                case RuntimeMoniker.CoreRt30:
                    return CoreRtToolchain.Core30;
                case RuntimeMoniker.CoreRt31:
                    return CoreRtToolchain.Core31;
                case RuntimeMoniker.CoreRt50:
                    return CoreRtToolchain.Core50;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtimeMoniker), runtimeMoniker, "RuntimeMoniker not supported");
            }
        }
    }
}