namespace BenchmarkDotNet.Toolchains.InProcess.Emit;

public record InProcessEmitSettings : InProcessSettings
{
    public static readonly InProcessEmitSettings Default = new();
}
