using System.Collections.Generic;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    internal interface IDisassemblyDiagnoser : IConfigurableDiagnoser<DisassemblyDiagnoserConfig>
    {
        IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> Results { get; }
    }
}