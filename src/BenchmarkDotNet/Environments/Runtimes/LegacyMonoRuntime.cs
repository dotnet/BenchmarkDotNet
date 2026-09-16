namespace BenchmarkDotNet.Environments;

/// <summary>
/// A legacy (based on .Net Framework) Mono runtime.
/// </summary>
public abstract class LegacyMonoRuntime : Runtime
{
    public sealed override Version? Version => null;
}
