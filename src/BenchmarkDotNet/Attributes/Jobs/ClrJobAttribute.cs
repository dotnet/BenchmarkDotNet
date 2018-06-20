using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class ClrJobAttribute : JobConfigBaseAttribute
    {
        public ClrJobAttribute(bool baseline = false) : base(Job.Clr.WithBaseline(baseline))
        {
        }
    }
}