using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Tests.Infra;
using Perfolizer.Helpers;
using Perfolizer.Models;
using VerifyXunit;
using Xunit;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

[Collection("VerifyTests")]
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
            var cpu = new CpuInfo
            {
                ProcessorName = processorName,
                PhysicalProcessorCount = physicalProcessorCount,
                PhysicalCoreCount = physicalCoreCount,
                LogicalCoreCount = logicalCoreCount,
            };

            captions.AppendLine(cpu.ToFullBrandName());
        }

        var settings = VerifyHelper.Create();
        return Verifier.Verify(captions.ToString(), settings);
    }
}