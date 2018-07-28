using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Specifies whether the common language runtime runs server garbage collection.
    /// <value>false: Does not run server garbage collection. This is the default.</value>
    /// <value>true: Runs server garbage collection.</value>
    /// </summary>
    [PublicAPI]
    public class GcServerAttribute : JobMutatorConfigBaseAttribute
    {
        public GcServerAttribute(bool value = false) : base(Job.Default.WithGcServer(value))
        {
        }
    }
}