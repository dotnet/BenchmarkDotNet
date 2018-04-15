namespace BenchmarkDotNet.Engines
{
    public interface IEngineFactory
    {
        IEngine Create(EngineParameters engineParameters);
    }
}