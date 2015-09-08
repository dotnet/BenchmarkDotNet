using BenchmarkDotNet.Samples.Algorithms;
using BenchmarkDotNet.Samples.CPU;
using BenchmarkDotNet.Samples.Framework;
using BenchmarkDotNet.Samples.IL;
using BenchmarkDotNet.Samples.Infra;
using BenchmarkDotNet.Samples.Introduction;
using BenchmarkDotNet.Samples.JIT;
using BenchmarkDotNet.Samples.Other;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var competitionSwitch = new BenchmarkCompetitionSwitch(new[] {
                // Introduction
                typeof(Intro_00_Basic),
                typeof(Intro_01_MethodTasks),
                typeof(Intro_02_ClassTasks),
                typeof(Intro_03_SingleRun),
                typeof(Intro_04_UniformReportingTest),
                // IL
                typeof(Il_ReadonlyFields),
                typeof(Il_Switch),
                // JIT
                typeof(Jit_LoopUnrolling),
                typeof(Jit_ArraySumLoopUnrolling),
                typeof(Jit_Inlining),
                typeof(Jit_BoolToInt),
                typeof(Jit_Bce),
                typeof(Jit_InterfaceMethod),
                typeof(Jit_RegistersVsStack),
                // CPU
                typeof(Cpu_Ilp_Inc),
                typeof(Cpu_Ilp_Max),
                typeof(Cpu_Ilp_VsBce),
                typeof(Cpu_Ilp_RyuJit),
                typeof(Cpu_MatrixMultiplication),
                typeof(Cpu_BranchPerdictor),
                // Framework
                typeof(Framework_SelectVsConvertAll),
                typeof(Framework_StackFrameVsStackTrace),
                typeof(Framework_StopwatchVsDateTime),
                // Algorithms
                typeof(Algo_BitCount),
                typeof(Algo_MostSignificantBit),
                typeof(Algo_Md5VsSha256),
                // Other
                typeof(Math_DoubleSqrt),
                typeof(Math_DoubleSqrtAvx),
                typeof(Array_HeapAllocVsStackAlloc),
                // Infra
                typeof(Infra_Params)
            });
            competitionSwitch.Run(args);
        }
    }
}
