using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Toolchains.Mono;
using BenchmarkDotNet.Toolchains.MonoWasm;
using BenchmarkDotNet.Toolchains.NativeAot;
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
                    if (RuntimeInformation.IsAndroid())
                        return InProcessEmitToolchain.Instance;
                    if (RuntimeInformation.IsiOS())
                        return InProcessNoEmitToolchain.Instance;
                    if (!string.IsNullOrEmpty(mono.AotArgs))
                        return MonoAotToolchain.Instance;

                    return RoslynToolchain.Instance;

                case CoreRuntime coreRuntime:
                    if (descriptor != null && descriptor.Type.Assembly.IsLinqPad())
                        return InProcessEmitToolchain.Instance;
                    if (coreRuntime.RuntimeMoniker != RuntimeMoniker.NotRecognized && !coreRuntime.IsPlatformSpecific)
                        return GetToolchain(coreRuntime.RuntimeMoniker);

                    return CsProjCoreToolchain.From(new NetCoreAppSettings(coreRuntime.MsBuildMoniker, null, coreRuntime.Name));

                case NativeAotRuntime nativeAotRuntime:
                    return nativeAotRuntime.RuntimeMoniker != RuntimeMoniker.NotRecognized
                            ? GetToolchain(nativeAotRuntime.RuntimeMoniker)
                            : NativeAotToolchain.CreateBuilder().UseNuGet().TargetFrameworkMoniker(nativeAotRuntime.MsBuildMoniker).ToToolchain();

                case WasmRuntime wasmRuntime:
                    return WasmToolchain.From(new NetCoreAppSettings(targetFrameworkMoniker: wasmRuntime.MsBuildMoniker, name: wasmRuntime.Name, runtimeFrameworkVersion: null));

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

                case RuntimeMoniker.Net481:
                    return CsProjClassicNetToolchain.Net481;

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
#pragma warning disable CS0618 // Type or member is obsolete
                case RuntimeMoniker.NetCoreApp50:
#pragma warning restore CS0618 // Type or member is obsolete
                case RuntimeMoniker.Net50:
                    return CsProjCoreToolchain.NetCoreApp50;

                case RuntimeMoniker.Net60:
                    return CsProjCoreToolchain.NetCoreApp60;

                case RuntimeMoniker.Net70:
                    return CsProjCoreToolchain.NetCoreApp70;

                case RuntimeMoniker.NativeAot60:
                    return NativeAotToolchain.Net60;

                case RuntimeMoniker.NativeAot70:
                    return NativeAotToolchain.Net70;

                default:
                    throw new ArgumentOutOfRangeException(nameof(runtimeMoniker), runtimeMoniker, "RuntimeMoniker not supported");
            }
        }
    }
}