using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class VeryLongRunJobAttribute : JobConfigBaseAttribute
    {
        public VeryLongRunJobAttribute() : base(Job.VeryLongRun)
        {
        }
    }
}