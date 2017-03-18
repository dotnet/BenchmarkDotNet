using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Reports
{
    public interface ISummaryStyle
    {
        bool PrintUnitsInHeader { get; set; }
        bool PrintUnitsInContent { get; set; }
        //MemoryUnit? MemoryUnit { get; set; }
        TimeUnit TimeUnit { get; set; }
    }
}