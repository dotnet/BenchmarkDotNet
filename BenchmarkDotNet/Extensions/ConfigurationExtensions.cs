using System.Linq;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Extensions
{
    internal static class ConfigurationExtensions
    {
        public static string ToConfig(this BenchmarkPlatform platform)
        {
            switch (platform)
            {
                case BenchmarkPlatform.AnyCpu:
                    return "AnyCPU";
                case BenchmarkPlatform.X86:
                    return "x86";
                case BenchmarkPlatform.X64:
                    return "x64";
                default:
                    return "AnyCPU";
            }
        }

        public static string ToConfig(this BenchmarkFramework framework)
        {
            var number = framework.ToString().Substring(1);
            var numberArray = number.ToCharArray().Select(c => c.ToString()).ToArray();
            return "v" + string.Join(".", numberArray);
        }

        public static string ToConfig(this BenchmarkJitVersion jitVersion)
        {
            return jitVersion == BenchmarkJitVersion.LegacyJit ? "1" : "0";
        }
    }
}