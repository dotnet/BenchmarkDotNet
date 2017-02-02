using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.Core;

namespace BenchmarkDotNet.Toolchains
{
    internal static class ToolchainExtensions
    {
        internal static IToolchain GetToolchain(this Job job)
        {
            return job.HasValue(InfrastructureMode.ToolchainCharacteristic)
                ? job.Infrastructure.Toolchain
                : GetToolchain(job.ResolveValue(EnvMode.RuntimeCharacteristic, EnvResolver.Instance));
        }

        internal static IToolchain GetToolchain(this Runtime runtime)
        {
            switch (runtime)
            {
                case Runtime.Clr:
                case Runtime.Mono:
#if CLASSIC
                    return new RoslynToolchain();
#else
                    return IsUsingProjectJson() ? new Classic.Net46Toolchain() : CsProj.CsProjToolchain.Net46;
#endif
                case Runtime.Core:
                    return IsUsingProjectJson() ? CoreToolchain.NetCoreApp11 : CsProj.CsProjToolchain.NetCoreApp11;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }

        private static bool IsUsingProjectJson()
            => HostEnvironmentInfo
                .GetCurrent()
                .DotNetCliVersion.Value
                .Contains("preview");
    }
}