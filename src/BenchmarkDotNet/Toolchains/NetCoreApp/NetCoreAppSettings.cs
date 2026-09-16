using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.NetCoreApp;

public sealed record NetCoreAppSettings : DotNetCliSettings
{
    public static readonly NetCoreAppSettings Default = new();

    public NetCoreAppSettings() { }

    internal NetCoreAppSettings(CommandLineOptions options) : base(options) { }
}
