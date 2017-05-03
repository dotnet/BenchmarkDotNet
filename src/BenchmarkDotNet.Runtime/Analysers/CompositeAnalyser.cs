using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class CompositeAnalyser : IAnalyser
    {
        private readonly IAnalyser[] analysers;
        private static int counter = 0; // TODO: improve

        public CompositeAnalyser(IAnalyser[] analysers)
        {
            this.analysers = analysers;
            Id = "Composite-" + (++counter);
        }

        public string Id { get; }
        public IEnumerable<Conclusion> Analyse(Summary summary) => analysers.SelectMany(analyser => analyser.Analyse(summary));
    }
}