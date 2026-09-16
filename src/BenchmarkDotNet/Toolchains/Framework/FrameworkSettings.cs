using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Framework;

public sealed record FrameworkSettings : DotNetCliSettings
{
    public static readonly FrameworkSettings Default = new();

    public FrameworkSettings() { }

    internal FrameworkSettings(CommandLineOptions options) : base(options) { }
}
