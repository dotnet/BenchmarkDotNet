using System.Diagnostics;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Framework
{
    public class Framework_StackFrameVsStackTrace
    {
        [Benchmark]
        public StackFrame StackFrame()
        {
            return new StackFrame(1, false);
        }

        [Benchmark]
        public StackFrame StackTrace()
        {
            return new StackTrace().GetFrame(1);
        }
    }
}