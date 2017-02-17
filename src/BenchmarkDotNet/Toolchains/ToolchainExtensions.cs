using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.ProjectJson;

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
                    return new Roslyn.RoslynToolchain();
#else
                    return IsUsingProjectJson() ? ProjectJsonNet46Toolchain.Instance : CsProjNet46Toolchain.Instance;
#endif
                case Runtime.Core:
                    return IsUsingProjectJson() ? ProjectJsonCoreToolchain.Current.Value : CsProjCoreToolchain.Current.Value;
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