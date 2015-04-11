using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class ReverseSortCompetition
    {
        private const int N = 6000000, RandSeed = 123;
        private readonly int[] originalData;
        private int[] data;

        public ReverseSortCompetition()
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

        [BenchmarkInitialize]
        public void LinqSortInitialize()
        {
            Initalize();
        }

        [BenchmarkInitialize]
        public void CompareToSortInitialize()
        {
            Initalize();
        }

        [BenchmarkInitialize]
        public void ComparerMinusSortInitialize()
        {
            Initalize();
        }

        [BenchmarkInitialize]
        public void ReverseSortInitialize()
        {
            Initalize();
        }

        [Benchmark("Linq")]
        public void LinqSort()
        {
            data = data.OrderByDescending(a => a).ToArray();
        }

        [Benchmark("CompareTo")]
        public void CompareToSort()
        {
            Array.Sort(data, (a, b) => a.CompareTo(b));
        }

        [Benchmark("(a,b)=>b-a")]
        public void ComparerMinusSort()
        {
            Array.Sort(data, (a, b) => b - a);
        }

        [Benchmark("Reverse")]
        public void ReverseSort()
        {
            Array.Sort(data);
            Array.Reverse(data);
        }
    }
}