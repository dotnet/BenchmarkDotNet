using BenchmarkDotNet;

namespace Benchmarks
{
    public class MakeRefVsBoxingCompetition : BenchmarkCompetition
    {
        private const int IterationCount = 100000000;
        private int[] array;

        protected override void Prepare()
        {
            array = new int[5];
        }

        [BenchmarkMethod("MakeRef")]
        public void MakeRef()
        {
            for (int i = 0; i < IterationCount; i++)
                Set1(array, 0, i);
        }

        [BenchmarkMethod("Boxing")]
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