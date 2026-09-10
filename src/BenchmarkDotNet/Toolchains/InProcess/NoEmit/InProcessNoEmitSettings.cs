namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

public record InProcessNoEmitSettings : InProcessSettings
{
    public static readonly InProcessNoEmitSettings Default = new();

    public IBenchmarkActionFactory? BenchmarkActionFactory { get; init; }
}
