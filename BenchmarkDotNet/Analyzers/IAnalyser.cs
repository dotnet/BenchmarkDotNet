using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analyzers
{
    public interface IAnalyser
    {
        IEnumerable<IWarning> Analyze(Summary summary);
    }
}