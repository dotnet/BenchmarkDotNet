namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var competitionSwitch = new BenchmarkCompetitionSwitch(new[] {
                typeof(Intro_00_Basic),
                typeof(Intro_01_MethodTasks),
                typeof(Intro_02_ClassTasks),
                typeof(Intro_03_SingleRun),
                typeof(Il_ReadonlyFields),
                typeof(Il_Switch),
                typeof(Jit_LoopUnrolling),
                typeof(Jit_ArraySumLoopUnrolling),
                typeof(Jit_Inlining),
                typeof(Jit_Bce),
                typeof(Jit_InterfaceMethod),
                typeof(Cpu_Ilp_Inc),
                typeof(Cpu_Ilp_Max),
                typeof(Cpu_Ilp_VsBce),
                typeof(Cpu_Ilp_RyuJit),
                typeof(Cpu_MatrixMultiplication),
                typeof(Framework_SelectVsConvertAll),
                typeof(Framework_StackFrameVsStackTrace),
                typeof(Math_DoubleSqrt),
                typeof(Math_DoubleSqrtAvx),
                typeof(Algo_BitCount)
            });
            competitionSwitch.Run(args);
        }
    }
}
