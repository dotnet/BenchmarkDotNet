using System;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Marks method to be executed after benchmark.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CleanupAttribute : Attribute
    {
        public CleanupAttribute()
        {
        }
    }
}
