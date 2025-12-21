namespace BenchmarkDotNet.Engines;

public class EngineFactory : IEngineFactory
{
    public IEngine Create(EngineParameters engineParameters)
        => new Engine(engineParameters);
}