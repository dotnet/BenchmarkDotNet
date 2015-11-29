using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Analyzers
{
    public class BenchmarkCompositeAnalyser : IBenchmarkAnalyser
    {
        public string Name => "composite";
        public string Description => "Composite Analyser";

        private readonly IBenchmarkAnalyser[] analysers;

        public BenchmarkCompositeAnalyser(IBenchmarkAnalyser[] analysers)
        {
            this.analysers = analysers;
        }

        public IEnumerable<IBenchmarkAnalysisWarning> Analyze(IEnumerable<BenchmarkReport> reports)
        {
            return analysers.SelectMany(analyser => analyser.Analyze(reports));
        }
    }
}