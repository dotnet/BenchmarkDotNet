namespace BenchmarkDotNet.Jobs
{
    public enum Runtime
    {
        Host,
        /// <summary>
        /// Desktop CLR
        /// </summary>
        Clr,
        Mono,
        /// <summary>
        /// Cross-platform Core CLR runtime
        /// </summary>
        Core
    }
}