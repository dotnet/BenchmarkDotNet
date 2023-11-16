using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
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
                    job.HasValue(InfrastructureMode.NuGetReferencesCharacteristic)
                    || job.HasValue(InfrastructureMode.BuildConfigurationCharacteristic)
                    || job.HasValue(InfrastructureMode.ArgumentsCharacteristic));

        internal static IToolchain GetToolchain(this Runtime runtime, Descriptor? descriptor = null, bool preferMsBuildToolchains = false)
        {
            switch (runtime)
            {
                case ClrRuntime clrRuntime:
                    if (!preferMsBuildToolchains && RuntimeInformation.IsFullFramework
                        && RuntimeInformation.GetCurrentRuntime().MsBuildMoniker == runtime.MsBuildMoniker)
                    {
                        return RoslynToolchain.Instance;
                    }

                    return clrRuntime.RuntimeMoniker != RuntimeMoniker.NotRecognized
                        ? GetToolchain(clrRuntime.RuntimeMoniker)
                        : CsProjClassicNetToolchain.From(clrRuntime.MsBuildMoniker);

                case MonoRuntime mono:
                    if (RuntimeInformation.IsAndroid())
                        return InProcessEmitToolchain.Instance;
                    if (RuntimeInformation.IsIOS())
                        return InProcessNoEmitToolchain.Instance;
                    if (!string.IsNullOrEmpty(mono.AotArgs))
                        return MonoAotToolchain.Instance;
                    if (mono.IsDotNetBuiltIn)
                        if (RuntimeInformation.IsNewMono)
                        {
                            // It's a .NET SDK with Mono as default VM.
                            // Publishing self-contained apps might not work like in https://github.com/dotnet/performance/issues/2787.
                            // In such case, we are going to use default .NET toolchain that is just going to perform dotnet build,
                            // which internally will result in creating Mono-based app.
                            return mono.RuntimeMoniker switch
                            {
                                RuntimeMoniker.Mono60 => GetToolchain(RuntimeMoniker.Net60),
                                RuntimeMoniker.Mono70 => GetToolchain(RuntimeMoniker.Net70),
                                RuntimeMoniker.Mono80 => GetToolchain(RuntimeMoniker.Net80),
                                RuntimeMoniker.Mono90 => GetToolchain(RuntimeMoniker.Net90),
                                _ => CsProjCoreToolchain.From(new NetCoreAppSettings(mono.MsBuildMoniker, null, mono.Name))
                            };
                        }
                        else
                        {
                            return MonoToolchain.From(
                                new NetCoreAppSettings(targetFrameworkMoniker: mono.MsBuildMoniker, runtimeFrameworkVersion: null, name: mono.Name));
                        }

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

                case RuntimeMoniker.Net80:
                    return CsProjCoreToolchain.NetCoreApp80;

                case RuntimeMoniker.Net90:
                    return CsProjCoreToolchain.NetCoreApp90;

                case RuntimeMoniker.NativeAot60:
                    return NativeAotToolchain.Net60;

                case RuntimeMoniker.NativeAot70:
                    return NativeAotToolchain.Net70;

                case RuntimeMoniker.NativeAot80:
                    return NativeAotToolchain.Net80;

                case RuntimeMoniker.NativeAot90:
                    return NativeAotToolchain.Net90;

                case RuntimeMoniker.Mono60:
                    return MonoToolchain.Mono60;

                case RuntimeMoniker.Mono70:
                    return MonoToolchain.Mono70;

                case RuntimeMoniker.Mono80:
                    return MonoToolchain.Mono80;

                case RuntimeMoniker.Mono90:
                    return MonoToolchain.Mono90;

                default:
                    throw new ArgumentOutOfRangeException(nameof(runtimeMoniker), runtimeMoniker, "RuntimeMoniker not supported");
            }
        }
    }
}