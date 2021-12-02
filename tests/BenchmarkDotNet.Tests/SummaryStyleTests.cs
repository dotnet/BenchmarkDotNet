using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
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
                cultureInfo: CultureInfo.InvariantCulture,
                printUnitsInHeader: true,
                printUnitsInContent: false,
                printZeroValuesInContent: true,
                allocationUnit: SizeUnit.B,
                codeSizeUnit: SizeUnit.B,
                timeUnit: TimeUnit.Millisecond
            );

            var config = ManualConfig.CreateEmpty().WithSummaryStyle(summaryStyle);

            Assert.Equal(CultureInfo.InvariantCulture, config.SummaryStyle.CultureInfo);
            Assert.True(config.SummaryStyle.PrintUnitsInHeader);
            Assert.False(config.SummaryStyle.PrintUnitsInContent);
            Assert.True(config.SummaryStyle.PrintZeroValuesInContent);
            Assert.Equal(SizeUnit.B, config.SummaryStyle.AllocationUnit);
            Assert.Equal(SizeUnit.B, config.SummaryStyle.CodeSizeUnit);
            Assert.Equal(TimeUnit.Millisecond, config.SummaryStyle.TimeUnit);
        }
    }
}