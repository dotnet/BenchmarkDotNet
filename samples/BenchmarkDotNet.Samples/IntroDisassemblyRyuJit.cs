using System.Linq;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [DisassemblyDiagnoser(printSource: true)]
    [RyuJitX64Job]
    public class IntroDisassemblyRyuJit
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