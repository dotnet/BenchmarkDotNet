using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// Benchmarks whose [Config] cannot be constructed. Reflection builds every attribute of a type in order to hand
    /// any of them back, so reading the attributes of these types throws, and it throws while the list of types is
    /// being built - before any benchmark of the assembly has been converted. They have to be dropped rather than
    /// take the discovery of every other benchmark down with them.
    /// </summary>
    public static class InvalidConfigProbe
    {
        /// <summary>
        /// ConfigAttribute instantiates the type it is given, and an abstract one cannot be instantiated.
        /// </summary>
        [Config(typeof(DebugConfig))]
        public class WithAbstractConfig
        {
            [Benchmark]
            public int Identity() => 1;
        }

        /// <summary>
        /// Same read, a different reason: the config has no public parameterless constructor.
        /// </summary>
        [Config(typeof(NoPublicConstructorConfig))]
        public class WithInaccessibleConfig
        {
            [Benchmark]
            public int Identity() => 1;

            private class NoPublicConstructorConfig : ManualConfig
            {
                private NoPublicConstructorConfig() { }
            }
        }
    }
}
