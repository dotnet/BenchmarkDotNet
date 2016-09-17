namespace BenchmarkDotNet.Environments
{
    public enum Runtime
    {
        /// <summary>
        /// Full .NET Framework (Windows only)
        /// </summary>
        Clr,

        /// <summary>
        /// Mono
        /// See also: http://www.mono-project.com/
        /// </summary>
        Mono,

        /// <summary>
        /// Cross-platform Core CLR runtime
        /// See also: https://docs.microsoft.com/en-us/dotnet/
        /// </summary>
        Core
    }
}