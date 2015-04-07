using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class ShiftVsMultiplyCompetition
    {
        private const int IterationCount = 1000000000;

        [BenchmarkMethod]
        public int Shift()
        {
            int value = 1;
            for (int i = 0; i < IterationCount; i++)
                value = value << 1;
            return value;
        }

        [BenchmarkMethod]
        public int Multiply()
        {
            int value = 1;
            for (int i = 0; i < IterationCount; i++)
                value = value * 2;
            return value;
        }
    }
}