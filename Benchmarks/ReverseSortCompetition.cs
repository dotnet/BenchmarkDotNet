using System;
using System.Linq;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class ReverseSortCompetition : BenchmarkCompetition
    {
        private const int N = 6000000, RandSeed = 123;
        private int[] originalData, data;

        protected override void Prepare()
        {
            originalData = new int[N];
            var random = new Random(RandSeed);
            for (int i = 0; i < N; i++)
                originalData[i] = random.Next() % 50;
        }

        private void Initalize()
        {
            data = (int[])originalData.Clone();
        }

        [BenchmarkMethodInitialize]
        public void LinqSortInitialize()
        {
            Initalize();
        }

        [BenchmarkMethodInitialize]
        public void CompareToSortInitialize()
        {
            Initalize();
        }

        [BenchmarkMethodInitialize]
        public void ComparerMinusSortInitialize()
        {
            Initalize();
        }

        [BenchmarkMethodInitialize]
        public void ReverseSortInitialize()
        {
            Initalize();
        }

        [BenchmarkMethod("Linq")]
        public void LinqSort()
        {
            data = data.OrderByDescending(a => a).ToArray();
        }

        [BenchmarkMethod("CompareTo")]
        public void CompareToSort()
        {
            Array.Sort(data, (a, b) => a.CompareTo(b));
        }

        [BenchmarkMethod("(a,b)=>b-a")]
        public void ComparerMinusSort()
        {
            Array.Sort(data, (a, b) => b - a);
        }

        [BenchmarkMethod("Reverse")]
        public void ReverseSort()
        {
            Array.Sort(data);
            Array.Reverse(data);
        }
    }
}