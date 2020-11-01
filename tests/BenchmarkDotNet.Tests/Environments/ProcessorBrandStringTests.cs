using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Portability.Cpu;
using Perfolizer.Horology;
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
        [InlineData("Intel(R) Core(TM) i7-8650U CPU @ 1.90GHz ", "Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R)")]
        [InlineData("Intel(R) Core(TM) i7-8700K CPU @ 3.70GHz", "Intel Core i7-8700K CPU 3.70GHz (Coffee Lake)")]
        public void IntelCoreIsPrettified(string originalName, string prettifiedName) =>
            Assert.Equal(prettifiedName, ProcessorBrandStringHelper.Prettify(new CpuInfo(originalName, nominalFrequency: null)));

        [Theory]
        [InlineData("Intel(R) Pentium(TM) G4560 CPU @ 3.50GHz", "Intel Pentium G4560 CPU 3.50GHz (Max: 3.70GHz)", 3.7)]
        [InlineData("Intel(R) Core(TM) i5-2500 CPU @ 3.30GHz", "Intel Core i5-2500 CPU 3.30GHz (Max: 3.70GHz) (Sandy Bridge)", 3.7)]
        [InlineData("Intel(R) Core(TM) i7-2600 CPU @ 3.40GHz", "Intel Core i7-2600 CPU 3.40GHz (Max: 3.70GHz) (Sandy Bridge)", 3.7)]
        [InlineData("Intel(R) Core(TM) i7-3770 CPU @ 3.40GHz", "Intel Core i7-3770 CPU 3.40GHz (Max: 3.50GHz) (Ivy Bridge)", 3.5)]
        [InlineData("Intel(R) Core(TM) i7-4770K CPU @ 3.50GHz", "Intel Core i7-4770K CPU 3.50GHz (Max: 3.60GHz) (Haswell)", 3.6)]
        [InlineData("Intel(R) Core(TM) i7-5775R CPU @ 3.30GHz", "Intel Core i7-5775R CPU 3.30GHz (Max: 3.40GHz) (Broadwell)", 3.4)]
        public void CoreIsPrettifiedWithDiffFrequencies(string originalName, string prettifiedName, double actualFrequency)
        {
            Assert.Equal(prettifiedName, ProcessorBrandStringHelper.Prettify(new CpuInfo(originalName, nominalFrequency: Frequency.FromGHz(actualFrequency)), includeMaxFrequency: true));
        }

        [Theory]
        [InlineData("AMD Ryzen 7 2700X Eight-Core Processor", "AMD Ryzen 7 2700X 4.10GHz", 4.1, 8, 16)]
        [InlineData("AMD Ryzen 7 2700X Eight-Core Processor", "AMD Ryzen 7 2700X Eight-Core Processor 4.10GHz", 4.1, null, null)]
        public void AmdIsPrettifiedWithDiffFrequencies(string originalName, string prettifiedName, double actualFrequency, int? physicalCoreCount, int? logicalCoreCount)
        {
            var cpuInfo = new CpuInfo(
                originalName,
                physicalProcessorCount: null,
                physicalCoreCount,
                logicalCoreCount,
                Frequency.FromGHz(actualFrequency),
                minFrequency: null,
                maxFrequency: null);

            Assert.Equal(prettifiedName, ProcessorBrandStringHelper.Prettify(cpuInfo, includeMaxFrequency: true));
        }

        [Theory]
        [InlineData("8130U", "Kaby Lake")]
        [InlineData("9900K", "Coffee Lake")]
        [InlineData("8809G", "Kaby Lake G")]
        public void IntelCoreMicroarchitecture(string modelNumber, string microarchitecture)
        {
            Assert.Equal(microarchitecture, ProcessorBrandStringHelper.ParseIntelCoreMicroarchitecture(modelNumber));
        }

        [Theory]
        [InlineData("", "Unknown processor")]
        [InlineData(null, "Unknown processor")]
        public void UnknownProcessorDoesNotThrow(string originalName, string prettifiedName)
        {
            var cpuInfo = new CpuInfo(originalName, nominalFrequency: null);

            Assert.Equal(prettifiedName, ProcessorBrandStringHelper.Prettify(cpuInfo, includeMaxFrequency: true));
        }
    }
}