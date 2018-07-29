using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class CompositeAnalyser : IAnalyser
    {
        private readonly IAnalyser[] _analysers;
        private static int counter; // TODO: improve

        public CompositeAnalyser(IAnalyser[] analysers)
        {
            this._analysers = analysers;
            Id = "Composite-" + ++counter;
        }

        public string Id { get; }
        public IEnumerable<Conclusion> Analyse(Summary summary) => _analysers.SelectMany(analyser => analyser.Analyse(summary));
    }
}