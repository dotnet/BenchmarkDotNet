using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Samples.CPU
{
    // See http://stackoverflow.com/questions/11227809/why-is-processing-a-sorted-array-faster-than-an-unsorted-array/11227902
    [HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
    [DisassemblyDiagnoser(printAsm: true, printSource: true)]
    public class Cpu_BranchPerdictor
    {
        private const int N = 32767;
        private readonly int[] sorted, unsorted;

        public Cpu_BranchPerdictor()
        {
            var random = new Random(0);
            unsorted = new int[N];
            sorted = new int[N];
            for (int i = 0; i < N; i++)
                sorted[i] = unsorted[i] = random.Next(256);
            Array.Sort(sorted);
        }

        [Benchmark]
        public int SortedBranch()
        {
            return Branch(sorted);
        }

        [Benchmark]
        public int UnsortedBranch()
        {
            return Branch(unsorted);
        }

        [Benchmark]
        public int SortedBranchless()
        {
            return Branchless(sorted);
        }

        [Benchmark]
        public int UnsortedBranchless()
        {
            return Branchless(unsorted);
        }

        private static int Branch(int[] data)
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
                if (data[i] >= 128)
                    sum += data[i];
            return sum;
        }

        private static int Branchless(int[] data)
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
            {
                int t = (data[i] - 128) >> 31;
                sum += ~t & data[i];
            }
            return sum;
        }
    }
}