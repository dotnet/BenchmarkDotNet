using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Analyzers
{
    public interface IBenchmarkAnalyser : IPlugin
    {
        IEnumerable<IBenchmarkAnalysisWarning> Analyze(IEnumerable<BenchmarkReport> reports);
    }
}