namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public enum ThresholdUnit
    {
        NotSet, // required because the command line parser library does not support nullable enums https://github.com/commandlineparser/commandline/issues/287
        Ratio,
        Nanoseconds,
        Microseconds,
        Milliseconds,
        Seconds,
        Minutes
    }
}