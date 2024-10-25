namespace BenchmarkDotNet.Configs
{
    public enum WakeLockType
    {
        /// <summary>
        /// Allows the system to enter sleep and/or turn off the display while benchmarks are running.
        /// </summary>
        No,

        /// <summary>
        /// Forces the system to be in the working state while benchmarks are running.
        /// </summary>
        RequireSystem,

        /// <summary>
        /// Forces the display to be on while benchmarks are running.
        /// </summary>
        RequireDisplay
    }
}