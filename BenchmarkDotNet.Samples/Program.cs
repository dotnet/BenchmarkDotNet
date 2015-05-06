namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var competitionSwitch = new BenchmarkCompetitionSwitch(new[] {
                typeof(Trivial_SimpleSum),
                typeof(Trivial_Increment),
                typeof(Trivial_ShiftVsMultiply),
                typeof(Jit_UnrollingArraySumLoop),
                typeof(Jit_Inlining),
                typeof(Jit_BoundsCheckingElimination),
                typeof(Jit_MultidimensionalArrayAccess),
                typeof(Cpu_MatrixMultiplication),
                typeof(Cpu_InstructionLevelParallelism),
                typeof(Framework_StringBuilder),
                typeof(Framework_ForeachArray),
                typeof(Framework_ForeachList),
                typeof(Framework_SelectVsConvertAll),
                typeof(Framework_StackFrameVsStackTrace),
                typeof(Algo_BitCount)
            });
            competitionSwitch.Run(args);
        }
    }
}
