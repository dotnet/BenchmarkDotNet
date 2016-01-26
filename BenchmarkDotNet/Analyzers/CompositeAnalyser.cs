using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analyzers
{
    public class CompositeAnalyser : IAnalyser
    {
        private readonly IAnalyser[] analysers;

        public CompositeAnalyser(IAnalyser[] analysers)
        {
            this.analysers = analysers;
        }

        public IEnumerable<IWarning> Analyze(Summary summary) => analysers.SelectMany(analyser => analyser.Analyze(summary));
    }
}