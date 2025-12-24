namespace BenchmarkDotNet.Toolchains.InProcess;

public abstract class InProcessSettings
{
    public bool ExecuteOnSeparateThread { get; set; } = true;
}