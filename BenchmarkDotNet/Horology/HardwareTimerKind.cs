namespace BenchmarkDotNet.Horology
{
    public enum HardwareTimerKind
    {
        /// <summary>
        /// Real-time clock
        /// <seealso cref="https://en.wikipedia.org/wiki/Real-time_clock"/>
        /// </summary>
        Rtc,

        /// <summary>
        /// Time Stamp Counter
        /// <seealso cref="https://en.wikipedia.org/wiki/Time_Stamp_Counter"/>
        /// </summary>
        Tsc,

        /// <summary>
        /// High Precision Event Timer
        /// <seealso cref="https://en.wikipedia.org/wiki/High_Precision_Event_Timer"/>
        /// </summary>
        Hpet
    }
}