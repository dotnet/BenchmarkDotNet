namespace BenchmarkDotNet.Engines
{
    public enum HostSignal
    {
        BeforeAnythingElse,
        AfterGlobalSetup,
        BeforeMainRun,
        BeforeGlobalCleanup,
        AfterAnythingElse
    }
}