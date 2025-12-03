using System;

namespace BenchmarkDotNet.Engines
{
    public interface IEngine : IDisposable
    {
        RunResults Run();
    }
}