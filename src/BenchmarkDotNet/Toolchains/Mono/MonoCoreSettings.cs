using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Mono;

public sealed record MonoCoreSettings : DotNetCliSettings
{
    public static readonly MonoCoreSettings Default = new();

    public MonoCoreSettings() { }

    internal MonoCoreSettings(CommandLineOptions options) : base(options) { }
}
