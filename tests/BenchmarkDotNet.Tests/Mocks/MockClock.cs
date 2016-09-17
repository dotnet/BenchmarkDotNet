using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Tests.Mocks
{
    public class MockClock : IClock
    {
        public MockClock(Frequency frequency)
        {
            Frequency = frequency;
        }

        public bool IsAvailable => true;
        public Frequency Frequency { get; }

        private long counter;
        public long GetTimestamp() => counter++;
    }
}