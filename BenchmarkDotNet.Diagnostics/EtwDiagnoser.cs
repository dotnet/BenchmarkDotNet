using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System.Collections.Generic;

namespace BenchmarkDotNet.Diagnostics
{
    public abstract class ETWDiagnoser
    {
        protected readonly List<int> ProcessIdsUsedInRuns = new List<int>();

        protected string GetSessionName(string prefix, Benchmark benchmark, ParameterInstances parameters = null)
        {
            if (parameters != null && parameters.Items.Count > 0)
                return $"{prefix}-{benchmark.ShortInfo}-{parameters.FullInfo}";
            return $"{prefix}-{benchmark.ShortInfo}";
        }
    }
}
