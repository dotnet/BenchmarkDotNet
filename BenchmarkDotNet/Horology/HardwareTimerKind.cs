namespace BenchmarkDotNet.Horology
{
    public enum HardwareTimerKind
    {
        /// <summary>
        /// System timer
        /// </summary>
        System,

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