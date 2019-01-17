using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.Roslyn;

namespace BenchmarkDotNet.Toolchains
{
    internal static class ToolchainExtensions
    {
        internal static IToolchain GetToolchain(this Job job)
            => job.HasValue(InfrastructureMode.ToolchainCharacteristic)
                ? job.Infrastructure.Toolchain
                : GetToolchain(job.ResolveValue(EnvironmentMode.RuntimeCharacteristic, EnvironmentResolver.Instance));

        internal static IToolchain GetToolchain(this Runtime runtime)
        {
            switch (runtime)
            {
                case ClrRuntime _:
                    if(RuntimeInformation.IsNetCore)
                        return CsProjClassicNetToolchain.Current.Value;

                    return RoslynToolchain.Instance;
                case MonoRuntime mono:
                    if(mono.AotArgs != null)
                        return new RoslynToolchain (mono);

                    return RoslynToolchain.Instance;
                case CoreRuntime _:
                    return CsProjCoreToolchain.Current.Value;
                case CoreRtRuntime _:
                    return CoreRtToolchain.LatestMyGetBuild;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }
    }
}