using System;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var competitionSwitch = new BenchmarkCompetitionSwitch(new[] {
                typeof(Trivial_Empty),
                typeof(Trivial_Sleep),
                typeof(Trivial_Increment),
                typeof(Jit_UnrollingArraySumLoop),
                typeof(Jit_Inlining),
                typeof(Jit_Bce),
                typeof(Jit_BceVsIlp),
                typeof(Cpu_MatrixMultiplication),
                typeof(Cpu_Ilp),
                typeof(Framework_SelectVsConvertAll),
                typeof(Framework_StackFrameVsStackTrace),
                typeof(Algo_BitCount)
            });
            competitionSwitch.Run(args);
        }
    }
}
