using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public interface IAnalyser
    {
        IEnumerable<IWarning> Analyse(Summary summary);
    }
}