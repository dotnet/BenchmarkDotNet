using System.Diagnostics;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class StackFrameProgram
    {
        private const int IterationCount = 100001;

        public void Run(Manager manager)
        {
            var competition = new BenchmarkCompetition();
            competition.AddTask("StackFrame", () => StackFrame());
            competition.AddTask("StackTrace", () => StackTrace());
            competition.Run();
            manager.ProcessCompetition(competition);
        }

        private StackFrame StackFrame()
        {
            StackFrame method = null;
            for (int i = 0; i < IterationCount; i++)
                method = new StackFrame(1, false);
            return method;
        }

        private StackFrame StackTrace()
        {
            StackFrame method = null;
            for (int i = 0; i < IterationCount; i++)
                method = new StackTrace().GetFrame(1);
            return method;
        }
    }
}