using System;
using System.Linq;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class ReverseSortProgram
    {
        private const int N = 6000000, RandSeed = 123;
        private int[] originalData, data;

        public void Run()
        {
            originalData = new int[N];
            var random = new Random(RandSeed);
            for (int i = 0; i < N; i++)
                originalData[i] = random.Next() % 50;

            var competition = new BenchmarkCompetition();
            competition.AddTask("Linq", Initalize, LinqSort);
            competition.AddTask("CompareTo", Initalize, CompareToSort);
            competition.AddTask("(a,b)=>b-a", Initalize, ComparerMinusSort);
            competition.AddTask("Reverse", Initalize, ReverseSort);
            competition.Run();
        }

        private void Initalize()
        {
            data = (int[])originalData.Clone();
        }

        public void LinqSort()
        {
            data = data.OrderByDescending(a => a).ToArray();
        }

        public void CompareToSort()
        {
            Array.Sort(data, (a, b) => a.CompareTo(b));
        }

        public void ComparerMinusSort()
        {
            Array.Sort(data, (a, b) => b - a);
        }

        public void ReverseSort()
        {
            Array.Sort(data);
            Array.Reverse(data);
        }
    }
}