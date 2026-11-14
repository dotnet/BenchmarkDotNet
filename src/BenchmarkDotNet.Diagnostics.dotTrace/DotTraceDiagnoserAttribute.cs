using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.dotTrace;

[AttributeUsage(AttributeTargets.Class)]
public class DotTraceDiagnoserAttribute : Attribute, IConfigSource
{
    public IConfig Config { get; }

    public DotTraceDiagnoserAttribute()
    {
        var diagnoser = new DotTraceDiagnoser();
        Config = ManualConfig.CreateEmpty().AddDiagnoser(diagnoser);
    }

    public DotTraceDiagnoserAttribute(bool profileSingleIteration, Uri? nugetUrl = null, string? downloadTo = null)
    {
        var diagnoser = new DotTraceDiagnoser(nugetUrl, downloadTo, profileSingleIteration);
        Config = ManualConfig.CreateEmpty().AddDiagnoser(diagnoser);
    }

    public DotTraceDiagnoserAttribute(Uri? nugetUrl, string? downloadTo = null)
    {
        var diagnoser = new DotTraceDiagnoser(nugetUrl, downloadTo);
        Config = ManualConfig.CreateEmpty().AddDiagnoser(diagnoser);
    }
}
