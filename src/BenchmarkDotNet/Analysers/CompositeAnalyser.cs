using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class CompositeAnalyser : IAnalyser
    {
        private readonly IAnalyser[] analysers;

        public CompositeAnalyser(IAnalyser[] analysers)
        {
            this.analysers = analysers;
        }

        public IEnumerable<IWarning> Analyse(Summary summary) => analysers.SelectMany(analyser => analyser.Analyse(summary));
    }
}