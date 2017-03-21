using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Reports
{
    public interface ISummaryStyle
    {
        bool PrintUnitsInHeader { get; set; }
        bool PrintUnitsInContent { get; set; }
        SizeUnit SizeUnit { get; set; }
        TimeUnit TimeUnit { get; set; }
    }
}