using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Reports
{
    public interface ISummaryStyle
    {
        bool PrintUnitsInHeader { get; }
        bool PrintUnitsInContent { get; }
        //MemoryUnit? MemoryUnit { get; }
        TimeUnit TimeUnit { get; }

        ISummaryStyle WithCurrentOrNewTimeUnit(TimeUnit newTimeUnit);
    }
}