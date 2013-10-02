using System.Diagnostics;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class StackFrameCompetition : BenchmarkCompetition
    {
        private const int IterationCount = 100001;

        [BenchmarkMethod]
        public StackFrame StackFrame()
        {
            StackFrame method = null;
            for (int i = 0; i < IterationCount; i++)
                method = new StackFrame(1, false);
            return method;
        }

        [BenchmarkMethod]
        public StackFrame StackTrace()
        {
            StackFrame method = null;
            for (int i = 0; i < IterationCount; i++)
                method = new StackTrace().GetFrame(1);
            return method;
        }
    }
}