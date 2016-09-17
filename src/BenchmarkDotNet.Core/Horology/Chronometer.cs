using System;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Horology
{
    public static class Chronometer
    {
        public static readonly IClock Stopwatch = new StopwatchClock();
        public static readonly IClock DateTime = new DateTimeClock();
        public static readonly IClock WindowsClock = new WindowsClock();

        public static readonly IClock BestClock;

        public static Frequency Frequency => BestClock.Frequency;
        public static long GetTimestamp() => BestClock.GetTimestamp();
        public static StartedClock Start() => BestClock.Start();
        public static TimeInterval GetResolution() => BestClock.GetResolution();

        static Chronometer()
        {
            if (RuntimeInformation.IsWindows() && WindowsClock.IsAvailable)
                BestClock = WindowsClock;
            else
                BestClock = Stopwatch;
        }

        public static HardwareTimerKind HardwareTimerKind => GetHardwareTimerKind(BestClock.Frequency);

        public static HardwareTimerKind GetHardwareTimerKind(Frequency frequency)
        {
            long freqKHz = (long)Math.Round(frequency / Frequency.KHz);
            if (14300 <= freqKHz && freqKHz <= 14400)
                return HardwareTimerKind.Hpet;
            if (3579500 <= frequency && frequency <= 3579600)
                return HardwareTimerKind.Acpi;
            if (freqKHz == 10000)
                return HardwareTimerKind.Unknown;
            if (5 <= freqKHz && freqKHz < 4000)
                return HardwareTimerKind.Tsc;
            if (freqKHz <= 4)
                return HardwareTimerKind.System;
            return HardwareTimerKind.Unknown;
        }
    }
}