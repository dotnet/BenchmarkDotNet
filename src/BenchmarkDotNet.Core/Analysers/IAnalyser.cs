using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public interface IAnalyser
    {
        string Id { get; }
        IEnumerable<Conclusion> Analyse(Summary summary);
    }
}