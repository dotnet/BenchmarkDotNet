namespace BenchmarkDotNet.Reports
{
    public interface IBenchmarkRunReportsStatistic
    {
        string Name { get; }
        IBenchmarkMeasurementStatistic Ticks { get; }
        IBenchmarkMeasurementStatistic Milliseconds { get; }
    }
}