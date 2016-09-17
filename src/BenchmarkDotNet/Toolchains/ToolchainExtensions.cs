using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Toolchains
{
    internal static class ToolchainExtensions
    {
        internal static IToolchain GetToolchain(this Job job)
        {
            return job.Infra.Toolchain.IsDefault
                ? GetToolchain(job.Env.Runtime.Resolve(EnvResolver.Instance))
                : job.Infra.Toolchain.SpecifiedValue;
        }

        internal static IToolchain GetToolchain(this Runtime runtime)
        {
            switch (runtime)
            {
                case Runtime.Clr:
                case Runtime.Mono:
                    return Classic.ClassicToolchain.Instance;
                case Runtime.Core:
                    return Core.CoreToolchain.Instance;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }
    }
}