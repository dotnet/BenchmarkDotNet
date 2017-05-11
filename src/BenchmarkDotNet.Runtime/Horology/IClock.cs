namespace BenchmarkDotNet.Horology
{
    public interface IClock
    {
        bool IsAvailable { get; }
        Frequency Frequency { get; }
        long GetTimestamp();
    }
}