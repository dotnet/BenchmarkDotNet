using BenchmarkDotNet.Environments;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Extensions
{
    public static class ConfigurationExtensions
    {
        [PublicAPI]
        public static string ToConfig(this Platform platform)
        {
            switch (platform)
            {
                case Platform.AnyCpu:
                    return "AnyCPU";
                case Platform.X86:
                    return "x86";
                case Platform.X64:
                    return "x64";
                case Platform.Arm:
                    return "ARM";
                case Platform.Arm64:
                    return "ARM64";
                default:
                    return "AnyCPU";
            }
        }

        public static string ToConfig(this Jit jit) => jit == Jit.LegacyJit ? "1" : "0";
    }
}
