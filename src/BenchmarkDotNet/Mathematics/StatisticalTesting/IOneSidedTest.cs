using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public interface IOneSidedTest<out T> where T : OneSidedTestResult
    {
        [CanBeNull]
        T IsGreater([NotNull] double[] x, [NotNull] double[] y, Threshold threshold = null);
    }
}