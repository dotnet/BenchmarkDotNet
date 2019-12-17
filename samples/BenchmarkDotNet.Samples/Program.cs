using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
    
    [DisassemblyDiagnoser(printAsm: true, printSource: true)] // !!! use the new diagnoser!!
    public class ThisOne
    {
        int[] field = Enumerable.Range(0, 100).ToArray();

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