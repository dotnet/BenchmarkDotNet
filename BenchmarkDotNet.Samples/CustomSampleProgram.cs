using System.Linq;

namespace BenchmarkDotNet.Samples
{
    // Benchmark methods are given explicitly
    public class CustomSampleProgram
    {
        private const int IterationCount = 10;
        private const int ArraySize = 1024 * 1024;

        private int[] array;

        public void Run()
        {
            var competition = new BenchmarkCompetition();
            competition.AddTask("For",
                () => { array = new int[ArraySize]; },
                () => For(),
                () => { array = null; });
            competition.AddTask("Linq",
                () => { array = new int[ArraySize]; },
                () => Linq(),
                () => { array = null; });
            competition.Run();
        }

        public int For()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < array.Length; i++)
                    sum += array[i];
            return sum;
        }

        public int Linq()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                sum += array.Sum();
            return sum;
        }
    }
}