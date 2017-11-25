namespace BenchmarkDotNet.Horology
{
    public interface IClock
    {
        string Title { get; }
        bool IsAvailable { get; }
        Frequency Frequency { get; }
        long GetTimestamp();
    }
}