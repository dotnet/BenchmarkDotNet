using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CsProj;

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
                case ClrRuntime clr:
                case MonoRuntime mono:
                    if(RuntimeInformation.IsNetCore)
                        return CsProjClassicNetToolchain.Current.Value;

                    return Roslyn.RoslynToolchain.Instance;
                case CoreRuntime core:
                    return CsProjCoreToolchain.Current.Value;
                case CoreRtRuntime coreRt:
                    return CoreRtToolchain.LatestMyGetBuild;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }
    }
}