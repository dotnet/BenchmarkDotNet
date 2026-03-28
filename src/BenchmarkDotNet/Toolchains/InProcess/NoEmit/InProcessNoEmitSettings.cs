namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

public class InProcessNoEmitSettings : InProcessSettings
{
    public IBenchmarkActionFactory? BenchmarkActionFactory { get; set; }
}