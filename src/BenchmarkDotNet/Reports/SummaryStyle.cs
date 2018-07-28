using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Reports
{
    public class SummaryStyle : ISummaryStyle
    {
        public bool PrintUnitsInHeader { get; set; } = false;
        public bool PrintUnitsInContent { get; set; } = true;
        public SizeUnit SizeUnit { get; set; } = null;
        public TimeUnit TimeUnit { get; set; } = null;

        public static SummaryStyle Default => new SummaryStyle()
        {
            PrintUnitsInHeader = false,
            PrintUnitsInContent = true,
            SizeUnit = null,
            TimeUnit = null
        };

        public ISummaryStyle WithTimeUnit(TimeUnit timeUnit)
        {
            return new SummaryStyle
            {
                PrintUnitsInHeader = PrintUnitsInHeader,
                PrintUnitsInContent = PrintUnitsInContent,
                SizeUnit = SizeUnit,
                TimeUnit = timeUnit
            };
        }

        public ISummaryStyle WithSizeUnit(SizeUnit sizeUnit)
        {
            return new SummaryStyle
            {
                PrintUnitsInHeader = PrintUnitsInHeader,
                PrintUnitsInContent = PrintUnitsInContent,
                SizeUnit = sizeUnit,
                TimeUnit = TimeUnit
            };
        }
    }
}
