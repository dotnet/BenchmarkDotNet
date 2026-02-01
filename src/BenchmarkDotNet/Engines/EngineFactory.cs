namespace BenchmarkDotNet.Engines;

#nullable enable

public class EngineFactory : IEngineFactory
{
    public IEngine Create(EngineParameters engineParameters)
        => new Engine(engineParameters);
}