using BenchmarkDotNet;

namespace Benchmarks
{
    public class StaticFieldProgram
    {
        private const int N = 1000, IterationCount = 1000000;

        private int[] nonStaticField;
        private static int[] staticField;

        public void Run()
        {
            nonStaticField = staticField = new int[N];

            var competition = new BenchmarkCompetition();
            competition.AddTask("Non-static", () => NonStaticRun());
            competition.AddTask("Static", () => StaticRun());
            competition.Run();
        }

        private int NonStaticRun()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    sum += nonStaticField[i];
            return sum;
        }

        private int StaticRun()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    sum += staticField[i];
            return sum;
        }
    }
}