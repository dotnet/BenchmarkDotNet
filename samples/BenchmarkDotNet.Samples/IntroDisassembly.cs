using BenchmarkDotNet.Attributes;
using System.Linq;

namespace BenchmarkDotNet.Samples
{
    [DisassemblyDiagnoser]
    public class IntroDisassembly
    {
        private int[] field = Enumerable.Range(0, 100).ToArray();

        [Benchmark]
        public int SumLocal()
        {
            var local = field; // we use local variable that points to the field

            int sum = 0;
            for (int i = 0; i < local.Length; i++)
                sum += local[i];

            return sum;
        }

        [Benchmark]
        public int SumField()
        {
            int sum = 0;
            for (int i = 0; i < field.Length; i++)
                sum += field[i];

            return sum;
        }
    }
}