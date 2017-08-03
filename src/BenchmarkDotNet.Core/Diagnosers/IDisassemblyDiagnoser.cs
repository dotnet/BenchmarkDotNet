using System.Collections.Generic;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    internal interface IDisassemblyDiagnoser : IDiagnoser
    {
        IReadOnlyDictionary<Benchmark, DisassemblyResult> Results { get; }
    }
}