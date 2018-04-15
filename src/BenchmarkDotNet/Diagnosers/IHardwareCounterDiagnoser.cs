using System.Collections.Generic;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    internal interface IHardwareCountersDiagnoser : IDiagnoser
    {
        IReadOnlyDictionary<Benchmark, PmcStats> Results { get; }
    }
}