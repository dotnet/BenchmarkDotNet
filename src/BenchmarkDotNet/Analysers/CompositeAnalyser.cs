using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class CompositeAnalyser : IAnalyser
    {
        private readonly ImmutableHashSet<IAnalyser> analysers;

        public CompositeAnalyser(ImmutableHashSet<IAnalyser> analysers) => this.analysers = analysers;

        public string Id => nameof(CompositeAnalyser);

        public IEnumerable<Conclusion> Analyse(Summary summary)
            => analysers.SelectMany(analyser => analyser.Analyse(summary));
    }
}