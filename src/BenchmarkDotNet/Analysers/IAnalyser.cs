using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Analysers
{
    public interface IAnalyser
    {
        [PublicAPI] string Id { get; }
        [PublicAPI] IEnumerable<Conclusion> Analyse(Summary summary);
    }
}