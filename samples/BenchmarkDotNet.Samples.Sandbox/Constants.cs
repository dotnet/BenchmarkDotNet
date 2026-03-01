using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet;

internal class Constants
{
    public static readonly IToolchain DefaultToolchain = CsProjCoreToolchain.NetCoreApp10_0;

    public class EnvironmentVariables
    {
        public const string BenchmarkDotNetConfig = "BENCHMARKDOTNET_CONFIG";
    }
}

internal class Categories
{
    // Categories that is used to filter benchmarks.
    public static class Filters
    {
        public const string NET8_0_OR_GREATER = "NET8_0_OR_GREATER";
        public const string NET9_0_OR_GREATER = "NET9_0_OR_GREATER";
        public const string NET10_0_OR_GREATER = "NET10_0_OR_GREATER";
    }
}
