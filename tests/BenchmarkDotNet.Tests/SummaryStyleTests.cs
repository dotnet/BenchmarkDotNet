using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Reports;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class SummaryStyleTests
    {
        [Fact]
        public void UserCanDefineCustomSummaryStyle()
        {
            var summaryStyle = new SummaryStyle
            (
                printUnitsInHeader: true,
                printUnitsInContent: false,
                sizeUnit: SizeUnit.B,
                timeUnit: TimeUnit.Millisecond
            );

            var config = ManualConfig.CreateEmpty().With(summaryStyle);
            
            Assert.True(config.SummaryStyle.PrintUnitsInHeader);
            Assert.False(config.SummaryStyle.PrintUnitsInContent);
            Assert.Equal(SizeUnit.B, config.SummaryStyle.SizeUnit);
            Assert.Equal(TimeUnit.Millisecond, config.SummaryStyle.TimeUnit);
        }
    }
}