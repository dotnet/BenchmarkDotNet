using BenchmarkDotNet.Jobs;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Specifies which outliers should be removed from the distribution
    /// </summary>
    public class OutliersAttribute : JobMutatorConfigBaseAttribute
    {
        public OutliersAttribute(OutlierMode outlierMode) : base(Job.Default.WithOutlierMode(outlierMode))
        {
        }
    }
}