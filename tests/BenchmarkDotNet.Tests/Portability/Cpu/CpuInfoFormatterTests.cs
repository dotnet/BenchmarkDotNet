using System.Runtime.CompilerServices;
using System.Text;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using BenchmarkDotNet.Portability.Cpu;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability.Cpu
{
    [Collection("ApprovalTests")]
    [UseReporter(typeof(XUnit2Reporter))]
    [UseApprovalSubdirectory("ApprovedFiles")]
    public class CpuInfoFormatterTests
    {
        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FormatTest()
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

            Approvals.Verify(captions.ToString());
        }
    }
}
