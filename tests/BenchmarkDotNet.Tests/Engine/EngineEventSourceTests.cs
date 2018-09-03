using BenchmarkDotNet.Engines;
using Xunit;

namespace BenchmarkDotNet.Tests.Engine
{
    public class EngineEventSourceTests
    {
        [Fact]
        public void SizeOfEnumsReportedToTracingMustEqualToIntegerSize()
        {
            Assert.Equal(sizeof(int), sizeof(IterationMode));
            Assert.Equal(sizeof(int), sizeof(IterationStage));
        }
    }
}