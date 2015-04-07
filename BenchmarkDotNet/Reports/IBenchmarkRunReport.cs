namespace BenchmarkDotNet.Reports
{
    public interface IBenchmarkRunReport
    {
        long Ticks { get; set; }
        long Milliseconds { get; set; }
    }
}