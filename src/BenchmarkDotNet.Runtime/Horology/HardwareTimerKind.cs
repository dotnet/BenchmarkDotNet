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
        /// <seealso href="https://en.wikipedia.org/wiki/Time_Stamp_Counter"/>
        /// </summary>
        Tsc,

        Acpi,

        /// <summary>
        /// High Precision Event Timer
        /// <seealso href="https://en.wikipedia.org/wiki/High_Precision_Event_Timer"/>
        /// </summary>
        Hpet,

        Unknown
    }
}