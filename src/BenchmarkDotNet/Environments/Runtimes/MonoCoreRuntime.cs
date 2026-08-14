using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Mono;
using BenchmarkDotNet.Toolchains.NetCoreApp;

namespace BenchmarkDotNet.Environments;

/// <summary>
/// .NET running on the Mono VM (built with <c>UseMonoRuntime=true</c>).
/// </summary>
public sealed class MonoCoreRuntime : Runtime
{
    public static readonly MonoCoreRuntime Net60 = new(new(6, 0));
    public static readonly MonoCoreRuntime Net70 = new(new(7, 0));
    public static readonly MonoCoreRuntime Net80 = new(new(8, 0));
    public static readonly MonoCoreRuntime Net90 = new(new(9, 0));
    public static readonly MonoCoreRuntime Net10_0 = new(new(10, 0));
    public static readonly MonoCoreRuntime Net11_0 = new(new(11, 0));

    private MonoCoreRuntime(Version version) => Version = version;

    public override string Name => "Mono with .NET";

    public override Version Version { get; }

    internal static MonoCoreRuntime GetCurrentVersion() => From(Environment.Version);

    /// <summary>Returns a runtime for the given version.</summary>
    public static MonoCoreRuntime From(Version version)
        => version.Major switch
        {
            6 => Net60,
            7 => Net70,
            8 => Net80,
            9 => Net90,
            10 => Net10_0,
            11 => Net11_0,
            _ => new(version),
        };

    public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
    {
        // A .NET SDK with Mono as the default VM. Publishing self-contained apps might not work
        // (https://github.com/dotnet/performance/issues/2787), so when the host is new Mono we use the default .NET
        // toolchain, which performs a plain dotnet build that internally produces a Mono-based app.
        if (RuntimeInformation.IsNewMono)
            return CsProjCoreToolchain.From(From(Version));

        return CsProjMonoCoreToolchain.From(this, MonoCoreSettings.Default);
    }
}
