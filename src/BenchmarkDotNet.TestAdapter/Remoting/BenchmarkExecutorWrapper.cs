using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.TestAdapter.Remoting
{
    /// <summary>
    /// A wrapper around the BenchmarkExecutor that works across AppDomain boundaries.
    /// </summary>
    internal class BenchmarkExecutorWrapper : MarshalByRefObject
    {
        private readonly BenchmarkExecutor benchmarkExecutor = new ();

        public void RunBenchmarks(string source, TestExecutionRecorderWrapper recorder, HashSet<Guid>? benchmarkIds = null)
        {
            benchmarkExecutor.RunBenchmarks(source, recorder, benchmarkIds);
        }

        public void Cancel()
        {
            benchmarkExecutor.Cancel();
        }
    }
}
