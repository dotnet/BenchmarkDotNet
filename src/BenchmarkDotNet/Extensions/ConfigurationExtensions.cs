using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Extensions
{
    internal static class ConfigurationExtensions
    {
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
                default:
                    return "AnyCPU";
            }
        }

        public static string ToConfig(this Jit jit)
        {
            return jit == Jit.LegacyJit ? "1" : "0";
        }
    }
}