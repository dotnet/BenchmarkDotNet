using BenchmarkDotNet;

namespace Benchmarks
{
    public class MakeRefVsBoxingProgram
    {
        private const int IterationCount = 100000000;
        private int[] array;

        public void Run(Manager manager)
        {
            array = new int[5];

            var competition = new BenchmarkCompetition();
            competition.AddTask("MakeRef", MakeRef);
            competition.AddTask("Boxing", Boxing);
            competition.Run();
            manager.ProcessCompetition(competition);
        }

        public void MakeRef()
        {
            for (int i = 0; i < IterationCount; i++)
                Set1(array, 0, i);
        }

        public void Boxing()
        {
            for (int i = 0; i < IterationCount; i++)
                Set2(array, 0, i);
        }

        public void Set1<T>(T[] a, int i, int v)
        {
            __refvalue(__makeref(a[i]), int) = v;
        }

        public void Set2<T>(T[] a, int i, int v)
        {
            a[i] = (T)(object)v;
        }
    }
}