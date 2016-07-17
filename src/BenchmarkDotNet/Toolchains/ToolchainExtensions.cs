using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains
{
    public static class ToolchainExtensions
    {
        public static IToolchain GetToolchain(this IJob job) => job.Toolchain ?? GetToolchain(job.Runtime);

        internal static IToolchain GetToolchain(this Runtime runtime)
        {
            switch (runtime)
            {
                case Runtime.Host:
                    return GetToolchain(RuntimeInformation.GetCurrent());
                case Runtime.Clr:
                case Runtime.Mono:
                    return Classic.ClassicToolchain.Instance;
                case Runtime.Core:
                    return Core.CoreToolchain.Instance;
            }

            throw new NotSupportedException("Runtime not supported");
        }
    }
}