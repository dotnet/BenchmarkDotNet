﻿using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

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
#if !UAP
                case Runtime.Clr:
                case Runtime.Mono:
                    return Classic.ClassicToolchain.Instance;
                case Runtime.Core:
                    return Core.CoreToolchain.Instance;
                case Runtime.Uap:
                    return Uap.UapToolchain.Instance;
#endif
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }
    }
}