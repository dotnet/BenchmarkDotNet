using BenchmarkDotNet;

namespace Benchmarks
{
    public class IncrementProgram
    {
        public void Run()
        {
            var competition = new BenchmarkCompetition();
            competition.AddTask("After", () => After());
            competition.AddTask("Before", () => Before());
            competition.Run();
        }

        private const int IterationCount = 2000000000;

        public static int After()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; i++)
                counter++;
            return counter;
        }

        public static int Before()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; ++i)
                ++counter;
            return counter;
        }
    }
}