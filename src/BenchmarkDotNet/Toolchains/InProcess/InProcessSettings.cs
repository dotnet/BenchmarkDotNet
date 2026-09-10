namespace BenchmarkDotNet.Toolchains.InProcess;

public abstract record InProcessSettings
{
    public bool ExecuteOnSeparateThread { get; init; } = true;
}
