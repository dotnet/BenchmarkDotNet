using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class ArrayIterationCompetition
    {
        private const int NUnroll = 1000, N = 1001, IterationCount = 1000000;

        private readonly int[] nonStaticField;
        private static int[] staticField;

        public ArrayIterationCompetition()
        {
            nonStaticField = staticField = new int[N];
        }

        [BenchmarkMethod("Non-static/unroll")]
        public int NonStaticUnroll()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < NUnroll; i++)
                    sum += nonStaticField[i];
            return sum;
        }

        [BenchmarkMethod("Static/unroll")]
        public int StaticUnroll()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < NUnroll; i++)
                    sum += staticField[i];
            return sum;
        }

        [BenchmarkMethod("Non-static")]
        public int NonStatic()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    sum += nonStaticField[i];
            return sum;
        }

        [BenchmarkMethod("Static")]
        public int Static()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    sum += staticField[i];
            return sum;
        }
    }
}