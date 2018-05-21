using System.Collections.Generic;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    internal interface IDisassemblyDiagnoser : IConfigurableDiagnoser<DisassemblyDiagnoserConfig>
    {
        IReadOnlyDictionary<Benchmark, DisassemblyResult> Results { get; }
    }
}