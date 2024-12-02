namespace BenchmarkDotNet.Configs
{
    public enum WakeLockType
    {
        /// <summary>
        /// Allows the system to enter sleep and/or turn off the display while benchmarks are running.
        /// </summary>
        None,

        /// <summary>
        /// Forces the system to be in the working state while benchmarks are running.
        /// </summary>
        System,

        /// <summary>
        /// Forces the display to be on while benchmarks are running.
        /// </summary>
        Display
    }
}