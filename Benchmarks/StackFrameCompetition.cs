using System.Diagnostics;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class StackFrameCompetition
    {
        private const int IterationCount = 100001;

        [Benchmark]
        public StackFrame StackFrame()
        {
            StackFrame method = null;
            for (int i = 0; i < IterationCount; i++)
                method = new StackFrame(1, false);
            return method;
        }

        [Benchmark]
        public StackFrame StackTrace()
        {
            StackFrame method = null;
            for (int i = 0; i < IterationCount; i++)
                method = new StackTrace().GetFrame(1);
            return method;
        }
    }
}