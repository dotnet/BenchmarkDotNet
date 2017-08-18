using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Toolchains
{
    internal static class ToolchainExtensions
    {
        internal static IToolchain GetToolchain(this Job job)
            => job.HasValue(InfrastructureMode.ToolchainCharacteristic)
                ? job.Infrastructure.Toolchain
                : GetToolchain(job.ResolveValue(EnvMode.RuntimeCharacteristic, EnvResolver.Instance));

        internal static IToolchain GetToolchain(this Runtime runtime)
        {
            switch (runtime)
            {
                case ClrRuntime clr:
                case MonoRuntime mono:
#if CLASSIC
                    return new Roslyn.RoslynToolchain();
#else
                    return CsProjClassicNetToolchain.Current.Value;
#endif
                case CoreRuntime core:
                    return CsProjCoreToolchain.Current.Value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }
    }
}