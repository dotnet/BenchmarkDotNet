namespace BenchmarkDotNet.Engines
{
    public enum HostSignal
    {
        BeforeAnythingElse,
        AfterSetup,
        BeforeCleanup,
        AfterAnythingElse
    }
}