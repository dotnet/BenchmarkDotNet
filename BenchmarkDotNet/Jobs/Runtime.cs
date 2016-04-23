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
        /// Desktop CLR hosted on Windows with Dot Net eXecution (DNX)
        /// </summary>
        Dnx,
        /// <summary>
        /// Cross-platform CoreCLR runtime
        /// </summary>
        Core
    }
}