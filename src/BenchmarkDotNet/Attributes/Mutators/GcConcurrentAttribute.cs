using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Specifies whether the common language runtime runs garbage collection on a separate thread.
    /// <value>false: Does not run garbage collection concurrently.</value>
    /// <value>true: Runs garbage collection concurrently. This is the default.</value>
    /// </summary>
    [PublicAPI]
    public class GcConcurrentAttribute : JobMutatorConfigBaseAttribute
    {
        public GcConcurrentAttribute(bool value = true) : base(Job.Default.WithGcConcurrent(value))
        {
        }
    }
}