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
            // Use fixed decimal precision for all numbers here so everything aligns nicely
            // in the tabular reports. Four decimal places seems a good compromise.
            // Note extra space between number and "s" to align that nicely too.
            if (Nanoseconds < 1000)
                return string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4} ns", Nanoseconds);
            if (Microseconds < 1000)
                return string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4} us", Microseconds);
            if (Milliseconds < 1000)
                return string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4} ms", Milliseconds);
            return string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4}  s", Seconds);
        }
    }
}