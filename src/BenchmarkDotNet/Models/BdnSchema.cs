using Perfolizer.Perfonar.Base;

namespace BenchmarkDotNet.Models;

internal class BdnSchema : PerfonarSchema
{
    public static readonly BdnSchema Instance = new();

    private BdnSchema() : base("bdn")
    {
        Add<BdnLifecycle>();
        Add<BdnHostInfo>();
        Add<BdnBenchmark>();
        Add<BdnEnvironment>();
        Add<BdnExecution>();
    }
}