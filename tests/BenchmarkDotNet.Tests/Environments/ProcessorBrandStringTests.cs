using BenchmarkDotNet.Environments;
using Xunit;

namespace BenchmarkDotNet.Tests.Environments
{
    public class ProcessorBrandStringTests
    {
        [Theory]
        [InlineData("Intel(R) Pentium(TM) G4560 CPU @ 3.50GHz", "Intel Pentium G4560 CPU 3.50GHz")]
        [InlineData("Intel(R) Core(TM) i7 CPU 970 @ 3.20GHz", "Intel Core i7 CPU 970 3.20GHz (Nehalem)")] // Nehalem/Westmere/Gulftown
        [InlineData("Intel(R) Core(TM) i7-920 CPU @ 2.67GHz", "Intel Core i7-920 CPU 2.67GHz (Nehalem)")]
        [InlineData("Intel(R) Core(TM) i7-2600 CPU @ 3.40GHz", "Intel Core i7-2600 CPU 3.40GHz (Sandy Bridge)")]
        [InlineData("Intel(R) Core(TM) i7-3770 CPU @ 3.40GHz", "Intel Core i7-3770 CPU 3.40GHz (Ivy Bridge)")]
        [InlineData("Intel(R) Core(TM) i7-4770K CPU @ 3.50GHz", "Intel Core i7-4770K CPU 3.50GHz (Haswell)")]
        [InlineData("Intel(R) Core(TM) i7-5775R CPU @ 3.30GHz", "Intel Core i7-5775R CPU 3.30GHz (Broadwell)")]
        [InlineData("Intel(R) Core(TM) i7-6700HQ CPU @ 2.60GHz", "Intel Core i7-6700HQ CPU 2.60GHz (Skylake)")]
        [InlineData("Intel(R) Core(TM) i7-7700 CPU @ 3.60GHz", "Intel Core i7-7700 CPU 3.60GHz (Kaby Lake)")]
        public void IntroCoreIsPrettified(string originalName, string prettifiedName) =>
            Assert.Equal(prettifiedName, ProcessorBrandStringHelper.Prettify(originalName));
    }
}