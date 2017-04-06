namespace BenchmarkDotNet.Engines
{
    public enum HostSignal
    {
        BeforeAnythingElse,
        AfterSetup,
        BeforeMainRun,
        BeforeCleanup,
        AfterAnythingElse
    }
}