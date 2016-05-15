using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Horology
{
    public static class Chronometer
    {
        public static readonly IClock Stopwatch = new StopwatchClock();
        public static readonly IClock DateTime = new DateTimeClock();
        public static readonly IClock WindowsClock = new WindowsClock();

        public static readonly IClock BestClock;

        public static long Frequency => BestClock.Frequency;
        public static long GetTimestamp() => BestClock.GetTimestamp();
        public static StartedClock Start() => BestClock.Start();
        public static double GetResolution(TimeUnit timeUnit = null) => BestClock.GetResolution(timeUnit);

        static Chronometer()
        {
            if (RuntimeInformation.IsWindows() && WindowsClock.IsAvailable)
                BestClock = WindowsClock;
            else
                BestClock = Stopwatch;
        }

        public static HardwareTimerKind HardwareTimerKind
        {
            get
            {
                if (System.Diagnostics.Stopwatch.IsHighResolution)
                {
                    var threshold = 10 * FrequencyUnit.MHz.HertzAmount;
                    if (Stopwatch.Frequency == threshold)
                        return HardwareTimerKind.Unknown;
                    return Stopwatch.Frequency >= 10 * FrequencyUnit.MHz.HertzAmount
                        ? HardwareTimerKind.Hpet
                        : HardwareTimerKind.Tsc;
                }
                return HardwareTimerKind.System;
            }
        }
    }
}