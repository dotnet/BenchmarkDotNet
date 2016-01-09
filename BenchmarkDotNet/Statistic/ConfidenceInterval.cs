namespace BenchmarkDotNet.Statistic
{
    // For now, it is always 95% CI
    public class ConfidenceInterval
    {
        public double Mean { get; }
        public double Error { get; }

        public double Lower { get; }
        public double Upper { get; }

        public ConfidenceInterval(double mean, double error)
        {
            Mean = mean;
            Error = error;
            Lower = mean - error;
            Upper = mean + error;
        }

        public override string ToString()
        {
            return string.Format(EnvironmentInfo.MainCultureInfo, "[{0}; {1}]", Lower, Upper);
        }
    }
}