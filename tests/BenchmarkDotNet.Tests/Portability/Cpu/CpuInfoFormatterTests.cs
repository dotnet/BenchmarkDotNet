using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Tests.Builders;
using VerifyXunit;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability.Cpu
{
    [Collection("VerifyTests")]
    [UsesVerify]
    public class CpuInfoFormatterTests
    {
        [Fact]
        public Task FormatTest()
        {
            var captions = new StringBuilder();
            foreach (var processorName in new[] { null, "", "Intel" })
            foreach (var physicalProcessorCount in new int?[] { null, 0, 1, 2 })
            foreach (var physicalCoreCount in new int?[] { null, 0, 1, 2 })
            foreach (var logicalCoreCount in new int?[] { null, 0, 1, 2 })
            {
                var mockCpuInfo = new CpuInfo(processorName, physicalProcessorCount, physicalCoreCount, logicalCoreCount, null, null, null);
                captions.AppendLine(CpuInfoFormatter.Format(mockCpuInfo));
            }

            var settings = VerifySettingsFactory.Create();
            return Verifier.Verify(captions.ToString(), settings);
        }
    }
}
