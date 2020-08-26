using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Samples
{
    [Diagnostics.Windows.Configs.InliningDiagnoser(logFailuresOnly: false, allowedNamespaces: new[] { "BenchmarkDotNet.Samples" })]
    public class IntroInliningDiagnoser
    {
        [Benchmark]
        public int IterationTest()
        {
            int j = 0;
            for (int i = 0; i < short.MaxValue; ++i)
            {
                j = i + AddThree(i);
            }

            return j + ReturnFive() + AddThree(ReturnFive());
        }

        [Benchmark]
        public int SplitJoin()
            => string.Join(",", new string[1000]).Split(',').Length;

        private int ReturnFive()
        {
            return 5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AddThree(int a)
        {
            return a + 3;
        }
    }
}
