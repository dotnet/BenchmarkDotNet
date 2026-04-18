using Perfolizer.Horology;

namespace BenchmarkDotNet.Tests.Mocks
{
    public class MockClock : IClock
    {
        public MockClock(Frequency frequency)
        {
            Frequency = frequency;
        }

        public string Title => "Mock";
        public bool IsAvailable => true;
        public Frequency Frequency { get; }

        private long counter;
        public long GetTimestamp() => counter++;
    }
}