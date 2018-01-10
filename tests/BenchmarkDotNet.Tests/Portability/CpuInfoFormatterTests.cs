#if CLASSIC
using System;
using System.Text;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Helpers;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability
{
    [UseReporter(typeof(DiffReporter))]
    [UseApprovalSubdirectory("ApprovedFiles")]
    public class CpuInfoFormatterTests
    {
        [Fact]
        public void BuildProcessorInfoCaptionTest()
        {
            var captions = new StringBuilder();
            foreach (var processorName in new[] { null, "", "Intel" })
            foreach (var physicalProcessorCount in new int?[] { null, 0, 1, 2 })
            foreach (var physicalCoreCount in new int?[] { null, 0, 1, 2 })
            foreach (var logicalCoreCount in new int?[] { null, 0, 1, 2 })
                captions.AppendLine(CpuInfoFormatter.Format(new MockCpuInfo(processorName, physicalProcessorCount, physicalCoreCount, logicalCoreCount)));
            
            Approvals.Verify(captions.ToString());
        }

        private class MockCpuInfo : ICpuInfo
        {
            public MockCpuInfo(string processorName, int? physicalProcessorCount, int? physicalCoreCount, int? logicalCoreCount)
            {
                PhysicalCoreCount = physicalCoreCount;
                PhysicalProcessorCount = physicalProcessorCount;
                LogicalCoreCount = logicalCoreCount;
                ProcessorName = processorName;
            }

            public int? PhysicalCoreCount { get; }
            public int? PhysicalProcessorCount { get; }
            public int? LogicalCoreCount { get; }
            public string ProcessorName { get; }
        }
    }
}
#endif