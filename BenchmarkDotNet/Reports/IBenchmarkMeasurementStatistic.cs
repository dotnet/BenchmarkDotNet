namespace BenchmarkDotNet.Reports
{
    public interface IBenchmarkMeasurementStatistic
    {
        string Name { get; }
        long Min { get; }
        long Max { get; }
        long Median { get; }
        double StandardDeviation { get; }
        double Error { get; }
    }
}