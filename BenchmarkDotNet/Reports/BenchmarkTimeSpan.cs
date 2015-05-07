namespace BenchmarkDotNet.Reports
{
    public class BenchmarkTimeSpan
    {
        public double Nanoseconds { get; }
        public double Microseconds => Nanoseconds / 1000;
        public double Milliseconds => Nanoseconds / 1000 / 1000;
        public double Seconds => Nanoseconds / 1000 / 1000 / 1000;

        public BenchmarkTimeSpan(double nanoseconds)
        {
            Nanoseconds = nanoseconds;
        }

        public override string ToString()
        {
            if (Nanoseconds < 0.001)
                return string.Format(EnvironmentHelper.MainCultureInfo, "{0:0.000000} ns", Nanoseconds);
            if (Nanoseconds < 0.01)
                return string.Format(EnvironmentHelper.MainCultureInfo, "{0:0.00000} ns", Nanoseconds);
            if (Nanoseconds < 0.1)
                return string.Format(EnvironmentHelper.MainCultureInfo, "{0:0.0000} ns", Nanoseconds);
            if (Nanoseconds < 1)
                return string.Format(EnvironmentHelper.MainCultureInfo, "{0:0.000} ns", Nanoseconds);
            if (Nanoseconds < 100000)
                return string.Format(EnvironmentHelper.MainCultureInfo, "{0:0.00} ns", Nanoseconds);
            if (Milliseconds < 1000)
                return string.Format(EnvironmentHelper.MainCultureInfo, "{0:0.00} ms", Milliseconds);
            return string.Format(EnvironmentHelper.MainCultureInfo, "{0:0.00} s", Seconds);
        }
    }
}