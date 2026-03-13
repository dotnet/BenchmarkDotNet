using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet;

/// <summary>
/// This class provide assembly level benchmark config.
/// It's required because `Program.cs` and `launchSettings.json` is not used when running on VS TestExplorer.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
internal class AssemblyConfigProvider : Attribute, IConfigSource
{
    public static IConfig GetConfig() => config.Value;

    public IConfig Config => config.Value;

    private static readonly Lazy<IConfig> config = new(() =>
    {
        var configKey = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.BenchmarkDotNetConfig) ?? "";

        switch (configKey)
        {
            case "":
            case "Default":
                return new DefaultBenchmarkConfig();
            case "Debug":
                return new DebugBenchmarkConfig();
            case "DebugInProcess":
                return new DebugInProcessBenchmarkConfig();
            case "TargetFrameworks":
                return new TargetFrameworksBenchmarkConfig();
            default:
                throw new InvalidOperationException($"Unknown benchmark config key is specified: {configKey}");
        }
    });
}
