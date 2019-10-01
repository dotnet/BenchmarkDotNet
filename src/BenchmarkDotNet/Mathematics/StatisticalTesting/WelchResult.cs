namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class WelchResult : OneSidedTestResult
    {
        public double T { get; }
        public double Df { get; }

        public WelchResult(double x, double df, double pValue, Threshold threshold) : base(pValue, threshold)
        {
            T = x;
            Df = df;
        }
    }
}