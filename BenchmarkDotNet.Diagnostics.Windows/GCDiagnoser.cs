using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    [Obsolete(message)]
    public class GCDiagnoser : IDiagnoser, IColumnProvider
    {
        const string message = "The \"GCDiagnoser\" has been renamed, please us the \"MemoryDiagnoser\" instead (it has the same functionality)";

        public IEnumerable<IColumn> GetColumns
        {
            get { throw new InvalidOperationException(message); }
        }

        public void AfterBenchmarkHasRun(Benchmark benchmark, Process process)
        {
            throw new InvalidOperationException(message);
        }

        public void DisplayResults(ILogger logger)
        {
            throw new InvalidOperationException(message);
        }

        public void ProcessStarted(Process process)
        {
            throw new InvalidOperationException(message);
        }

        public void ProcessStopped(Process process)
        {
            throw new InvalidOperationException(message);
        }

        public void Start(Benchmark benchmark)
        {
            throw new InvalidOperationException(message);
        }

        public void Stop(Benchmark benchmark, BenchmarkReport report)
        {
            throw new InvalidOperationException(message);
        }
    }
}
