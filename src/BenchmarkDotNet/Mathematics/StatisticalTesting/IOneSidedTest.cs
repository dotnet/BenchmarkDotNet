using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public interface IOneSidedTest<T> where T : OneSidedTestResult
    {
        [CanBeNull]
        T IsGreater(double[] x, double[] y, Threshold threshold = null);
    }
}