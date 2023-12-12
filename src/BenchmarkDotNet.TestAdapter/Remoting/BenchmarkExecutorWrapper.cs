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

        public void RunBenchmarks(string assemblyPath, TestExecutionRecorderWrapper recorder, HashSet<Guid>? benchmarkIds = null)
        {
            benchmarkExecutor.RunBenchmarks(assemblyPath, recorder, benchmarkIds);
        }

        public void Cancel()
        {
            benchmarkExecutor.Cancel();
        }
    }
}
