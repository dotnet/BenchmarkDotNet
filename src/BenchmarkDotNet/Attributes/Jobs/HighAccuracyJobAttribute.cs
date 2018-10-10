using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class HighAccuracyJobAttribute : JobConfigBaseAttribute
    {
        public HighAccuracyJobAttribute() : base(Job.HighAccuracy)
        {
        }
    }
}
