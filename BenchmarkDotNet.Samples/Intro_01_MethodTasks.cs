using System;
using System.Threading;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    // For some methods, logic may differ for different environments.
    public class Intro_01_MethodTasks
    {
        [Benchmark]
        // In this case, you can declare several tasks with the Task attribute.
        // For example, we can run the benchmark methods for the x86 and x64 platforms.
        [BenchmarkTask(platform: BenchmarkPlatform.X86)]
        [BenchmarkTask(platform: BenchmarkPlatform.X64)]
        public void Foo()
        {
            // x86: IntPtr.Size == 4
            // x64: IntPtr.Size == 8
            for (int i = 0; i < IntPtr.Size; i++)
                Thread.Sleep(100);
        }
    }
}