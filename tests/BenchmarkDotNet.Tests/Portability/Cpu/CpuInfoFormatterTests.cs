#if CLASSIC || NETCOREAPP2_0
using System.Text;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Tests.Mocks;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability.Cpu
{   
    [Collection("ApprovalTests")]
    [UseReporter(typeof(XUnit2Reporter))]
    [UseApprovalSubdirectory("ApprovedFiles")]
    public class CpuInfoFormatterTests
    {
        [Fact]
        public void FormatTest()
        {
            var captions = new StringBuilder();
            foreach (var processorName in new[] { null, "", "Intel" })
            foreach (var physicalProcessorCount in new int?[] { null, 0, 1, 2 })
            foreach (var physicalCoreCount in new int?[] { null, 0, 1, 2 })
            foreach (var logicalCoreCount in new int?[] { null, 0, 1, 2 })
            {
                var mockCpuInfo = new MockCpuInfo
                {
                    ProcessorName = processorName,
                    PhysicalProcessorCount = physicalProcessorCount,
                    PhysicalCoreCount = physicalCoreCount,
                    LogicalCoreCount = logicalCoreCount
                };
                captions.AppendLine(CpuInfoFormatter.Format(mockCpuInfo));
            }

            Approvals.Verify(captions.ToString());
        }
    }
}
#endif