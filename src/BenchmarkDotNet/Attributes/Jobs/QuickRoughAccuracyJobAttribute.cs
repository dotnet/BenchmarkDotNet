using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class QuickRoughAccuracyJobAttribute : JobConfigBaseAttribute
    {
        public QuickRoughAccuracyJobAttribute() : base(Job.QuickRough)
        {
        }
    }
}
