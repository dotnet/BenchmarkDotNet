using System.Linq;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Samples;

namespace Benchmarks
{
    public class AttributesSample : ISample
    {
        public void Run()
        {
            new BenchmarkRunner().RunCompetition(new AttributesSampleCompetition());
        }

        public class AttributesSampleCompetition
        {
            private const int IterationCount = 10;
            private const int ArraySize = 1024 * 1024;

            private int[] array;

            [BenchmarkInitialize]
            public void ForInitialize()
            {
                array = new int[ArraySize];
            }

            [Benchmark]
            public int For()
            {
                int sum = 0;
                for (int iteration = 0; iteration < IterationCount; iteration++)
                    for (int i = 0; i < array.Length; i++)
                        sum += array[i];
                return sum;
            }

            [BenchmarkClean]
            public void ForClean()
            {
                array = null;
            }

            [BenchmarkInitialize]
            public void LinqInitialize()
            {
                array = new int[ArraySize];
            }

            [Benchmark]
            public int Linq()
            {
                int sum = 0;
                for (int iteration = 0; iteration < IterationCount; iteration++)
                    sum += array.Sum();
                return sum;
            }

            [BenchmarkClean]
            public void LinqClean()
            {
                array = null;
            }
        }
    }

}