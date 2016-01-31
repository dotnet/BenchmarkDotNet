namespace BenchmarkDotNet.Horology
{
    public interface IClock
    {
        bool IsAvailable { get; }
        long Frequency { get; }
        long GetTimestamp();
    }
}