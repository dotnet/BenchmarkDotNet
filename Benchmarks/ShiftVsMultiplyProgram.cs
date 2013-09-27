using BenchmarkDotNet;

namespace Benchmarks
{
    public class ShiftVsMultiplyProgram
    {
        private const int IterationCount = 1000000000;

        public void Run(Manager manager)
        {
            var competition = new BenchmarkCompetition();
            competition.AddTask("Shift", () => Shift());
            competition.AddTask("Multiply", () => Multiply());
            competition.Run();
            manager.ProcessCompetition(competition);
        }

        public int Shift()
        {
            int value = 1;
            for (int i = 0; i < IterationCount; i++)
                value = value << 1;
            return value;
        }

        public int Multiply()
        {
            int value = 1;
            for (int i = 0; i < IterationCount; i++)
                value = value * 2;
            return value;
        }
    }
}