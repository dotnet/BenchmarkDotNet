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
    }
}