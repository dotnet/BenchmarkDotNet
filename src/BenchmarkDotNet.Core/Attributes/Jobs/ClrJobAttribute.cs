using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class ClrJobAttribute : JobConfigBaseAttribute
    {
        public ClrJobAttribute(bool isBaseline = false) : base(Job.Clr.WithIsBaseline(isBaseline))
        {
        }
    }
}