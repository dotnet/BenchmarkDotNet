using Perfolizer.Phd.Base;

namespace BenchmarkDotNet.Phd;

public class BdnSchema : PhdSchema
{
    public static readonly BdnSchema Instance = new ();

    private BdnSchema() : base("bdn")
    {
        Add<BdnInfo>();
        Add<BdnLifecycle>();
        Add<BdnHost>();
        Add<BdnBenchmark>();
        Add<BdnJob>();
        Add<BdnEnvironment>();
        Add<BdnExecution>();
    }
}