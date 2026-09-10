using BenchmarkDotNet.ConsoleArguments;

namespace BenchmarkDotNet.Toolchains.Mono;

public sealed record MonoSettings : LegacyMonoSettings
{
    public static readonly MonoSettings Default = new();

    public MonoSettings() { }

    internal MonoSettings(CommandLineOptions options) : base(options) { }
}
