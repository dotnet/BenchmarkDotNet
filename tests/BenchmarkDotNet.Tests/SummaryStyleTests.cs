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
        public void UserCanDefineCusomSummaryStyle()
        {
            var summaryStyle = new SummaryStyle
            {
                PrintUnitsInHeader = true,
                PrintUnitsInContent = false,
                SizeUnit = SizeUnit.B,
                TimeUnit = TimeUnit.Millisecond
            };

            var config = ManualConfig.CreateEmpty().With(summaryStyle);
            
            Assert.True(config.GetSummaryStyle().PrintUnitsInHeader);
            Assert.False(config.GetSummaryStyle().PrintUnitsInContent);
            Assert.Equal(SizeUnit.B, config.GetSummaryStyle().SizeUnit);
            Assert.Equal(TimeUnit.Millisecond, config.GetSummaryStyle().TimeUnit);
        }
    }
}