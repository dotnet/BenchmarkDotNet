using System.Globalization;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class OneSidedTestResult
    {
        public double PValue { get; }
        public Threshold Threshold { get; }
        public bool NullHypothesisIsRejected => PValue < 0.05;

        public OneSidedTestResult(double pValue, Threshold threshold)
        {
            PValue = pValue;
            Threshold = threshold;
        }

        public string H0 => Threshold.IsZero()
            ? "True difference in means is zero"
            : $"True difference in means <= {Threshold}";

        public string H1 => Threshold.IsZero()
            ? "True difference in means is greater than zero"
            : $"True difference in means > {Threshold}";

        public string PValueStr => PValue.ToString("N4", DefaultCultureInfo.Instance);
    }
}