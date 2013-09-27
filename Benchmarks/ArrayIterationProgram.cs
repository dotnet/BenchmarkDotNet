using BenchmarkDotNet;

namespace Benchmarks
{
    public class ArrayIterationProgram
    {
        private const int NUnroll = 1000, N = 1001, IterationCount = 1000000;

        private int[] nonStaticField;
        private static int[] staticField;

        public void Run(Manager manager)
        {
            nonStaticField = staticField = new int[N];

            var competition = new BenchmarkCompetition();
            competition.AddTask("Non-static/unroll", () => NonStaticUnrollRun());
            competition.AddTask("Static/unroll", () => StaticUnrollRun());
            competition.AddTask("Non-static", () => NonStaticRun());
            competition.AddTask("Static", () => StaticRun());
            competition.Run();
            manager.ProcessCompetition(competition);
        }

        private int NonStaticUnrollRun()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < NUnroll; i++)
                    sum += nonStaticField[i];
            return sum;
        }

        private int StaticUnrollRun()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < NUnroll; i++)
                    sum += staticField[i];
            return sum;
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