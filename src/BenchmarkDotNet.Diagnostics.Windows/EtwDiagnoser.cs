using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System.Collections.Generic;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public abstract class ETWDiagnoser
    {
        protected readonly List<int> ProcessIdsUsedInRuns = new List<int>();

        protected string GetSessionName(string prefix, Benchmark benchmark, ParameterInstances parameters = null)
        {
            if (parameters != null && parameters.Items.Count > 0)
                return $"{prefix}-{benchmark.FolderInfo}-{parameters.FolderInfo}";
            return $"{prefix}-{benchmark.FolderInfo}";
        }
    }
}
