using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Specifies whether the BenchmarkDotNet's benchmark runner forces full garbage collection after each benchmark invocation
    /// <value>false: Does not force garbage collection.</value>
    /// <value>true: Forces full garbage collection after each benchmark invocation. This is the default.</value>
    /// </summary>
    public class GcForceAttribute : JobMutatorConfigBaseAttribute
    {
        public GcForceAttribute(bool value = true) : base(Job.Default.WithGcForce(value))
        {
        }
    }
}