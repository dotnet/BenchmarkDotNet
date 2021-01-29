using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Disassemblers
{
    internal class DisassemblyAnalyzer : IAnalyser
    {
        public string Id => "Disassembly";

        private readonly IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results;

        internal DisassemblyAnalyzer(IReadOnlyDictionary<BenchmarkCase, DisassemblyResult> results) => this.results = results;

        public IEnumerable<Conclusion> Analyse(Summary summary)
            => from pair in results
                from error in pair.Value.Errors
                select Conclusion.CreateWarning(Id, error, summary[pair.Key]);
    }
}