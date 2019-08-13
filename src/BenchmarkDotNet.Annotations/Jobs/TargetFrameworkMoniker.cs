namespace BenchmarkDotNet.Jobs
{
    public enum TargetFrameworkMoniker
    {
        /// <summary>
        /// .NET 4.6.1
        /// </summary>
        Net461,
        /// <summary>
        /// .NET 4.6.2
        /// </summary>
        Net462,
        /// <summary>
        /// .NET 4.7
        /// </summary>
        Net47,
        /// <summary>
        /// .NET 4.7.1
        /// </summary>
        Net471,
        /// <summary>
        /// .NET 4.7.2
        /// </summary>
        Net472,
        /// <summary>
        /// .NET 4.8
        /// </summary>
        Net48,
        /// <summary>
        /// .NET Core 2.0
        /// </summary>
        NetCoreApp20,
        /// <summary>
        /// .NET Core 2.1
        /// </summary>
        NetCoreApp21,
        /// <summary>
        /// .NET Core 2.2
        /// </summary>
        NetCoreApp22,
        /// <summary>
        /// .NET Core 3.0
        /// </summary>
        NetCoreApp30,
        /// <summary>
        /// .NET Core 3.1
        /// </summary>
        NetCoreApp31,
        /// <summary>
        /// .NET Core 5.0 aka ".NET 5"
        /// </summary>
        NetCoreApp50,
        /// <summary>
        /// Mono
        /// </summary>
        Mono,
        /// <summary>
        /// CoreRT
        /// </summary>
        CoreRt,
    }
}
