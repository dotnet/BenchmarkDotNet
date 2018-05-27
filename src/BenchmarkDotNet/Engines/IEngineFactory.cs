namespace BenchmarkDotNet.Engines
{
    public interface IEngineFactory
    {
        IEngine CreateReadyToRun(EngineParameters engineParameters);
    }
}