using System.Diagnostics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public abstract class JitDiagnoser : EtwDiagnoser<object>, IDiagnoser
    {
        protected override ulong EventType => (ulong)ClrTraceEventParser.Keywords.JitTracing;

        protected override string SessionNamePrefix => "JitTracing";

        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;

        public void BeforeAnythingElse(Process process, Benchmark benchmark) => Start(process, benchmark);

        public void AfterSetup(Process process, Benchmark benchmark) { }

        public void BeforeMainRun(Process process, Benchmark benchmark) { }

        public void BeforeCleanup() => Stop();

        public virtual void ProcessResults(Benchmark benchmark, BenchmarkReport report) { }

        public void DisplayResults(ILogger outputLogger)
        {
            if (Logger.CapturedOutput.Count > 0)
                outputLogger.WriteLineHeader(new string('-', 20));
            foreach (var line in Logger.CapturedOutput)
                outputLogger.Write(line.Kind, line.Text);
        }
    }
}