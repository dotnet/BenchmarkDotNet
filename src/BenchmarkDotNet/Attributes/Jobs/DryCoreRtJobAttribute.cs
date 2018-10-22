using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class DryCoreRtJobAttribute : JobConfigBaseAttribute
    {
        public DryCoreRtJobAttribute() : base(Job.DryCoreRT) { }
    }
}