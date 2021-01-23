using BenchmarkDotNet.Jobs;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// specifies whether Engine should allocate some random-sized memory between iterations
    /// <remarks>it makes [GlobalCleanup] and [GlobalSetup] methods to be executed after every iteration</remarks>
    /// </summary>
    public class MemoryRandomizationAttribute : JobMutatorConfigBaseAttribute
    {
        public MemoryRandomizationAttribute(bool enable = true, OutlierMode outlierMode = OutlierMode.DontRemove)
            : base(Job.Default.WithMemoryRandomization(enable).WithOutlierMode(outlierMode))
        {
        }
    }
}
